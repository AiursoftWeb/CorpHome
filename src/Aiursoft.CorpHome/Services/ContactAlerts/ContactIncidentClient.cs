using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Services;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.CorpHome.Services.ContactAlerts;

public interface IContactIncidentClient
{
    Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request);
    Task MitigateAsync(Guid incidentId);
}

public sealed class ContactIncidentClient(AiurProtocolClient http, AlertCenterSettingsProvider settingsProvider) : IContactIncidentClient
{
    private Task<AlertCenterApiClient>? _client;

    private async Task<AlertCenterApiClient> CreateClientAsync()
    {
        var settings = await settingsProvider.GetAsync();
        settings.Validate();
        return new AlertCenterApiClient(http, Options.Create(new AlertCenterApiOptions
        {
            Endpoint = settings.Endpoint.TrimEnd('/')
        }), new SnapshotKeyProvider(settings.ApiKey));
    }

    public async Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request) =>
        await (await (_client ??= CreateClientAsync())).CreateIncidentAsync(request);

    public async Task MitigateAsync(Guid incidentId)
    {
        var client = await (_client ??= CreateClientAsync());
        var current = await client.GetIncidentAsync(incidentId);
        if (current.Code != Code.JobDone || current.Incident.IncidentId != incidentId)
            throw new InvalidOperationException("AlertCenter did not return the requested incident.");
        if (current.Incident.Status is "mitigated" or "resolved") return;
        var receipt = await client.MitigateIncidentAsync(incidentId, new MitigateIncidentRequest
        {
            Version = current.Incident.Version,
            Reason = "CorpHome detected a staff email follow-up to the customer in the configured mailbox."
        });
        if (receipt.Code is not (Code.JobDone or Code.NoActionTaken) ||
            receipt.IncidentId != incidentId || receipt.MitigatedAtUtc == null)
            throw new InvalidOperationException("AlertCenter did not return a valid mitigation receipt.");
    }

    private sealed class SnapshotKeyProvider(string key) : IAlertCenterApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync() => ValueTask.FromResult(key);
    }
}
