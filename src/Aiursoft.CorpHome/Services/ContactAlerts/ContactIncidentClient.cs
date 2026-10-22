using Aiursoft.AiurProtocol.Services;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.CorpHome.Services.ContactAlerts;

public interface IContactIncidentClient
{
    Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request);
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

    private sealed class SnapshotKeyProvider(string key) : IAlertCenterApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync() => ValueTask.FromResult(key);
    }
}
