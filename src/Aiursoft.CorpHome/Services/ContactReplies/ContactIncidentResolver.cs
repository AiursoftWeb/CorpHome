using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Services;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Microsoft.Extensions.Options;

namespace Aiursoft.CorpHome.Services.ContactReplies;

public interface IContactIncidentResolver
{
    Task ResolveAsync(Contact contact);
}

public sealed class ContactIncidentResolver(AiurProtocolClient http, AlertCenterSettingsProvider settingsProvider) : IContactIncidentResolver
{
    public async Task ResolveAsync(Contact contact)
    {
        if (contact.AlertCenterReportId == null) return;
        if (contact.AlertCenterReportedAtUtc == null)
            throw new InvalidOperationException("The contact incident has not yet been reported.");
        var settings = await settingsProvider.GetAsync();
        if (!settings.Enabled) throw new InvalidOperationException("AlertCenter synchronization is paused.");
        settings.Validate();
        var client = new AlertCenterApiClient(http,
            Options.Create(new AlertCenterApiOptions { Endpoint = settings.Endpoint.TrimEnd('/') }),
            new ReplyKeyProvider(settings.ApiKey));
        IncidentDetails? match;
        for (var page = 1; ; page++)
        {
            var response = await client.QueryIncidentsAsync(new QueryIncidentsRequest
            {
                MetadataKey = "contactId", MetadataValue = contact.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Page = page, PageSize = 100
            });
            if (response.Code != Code.JobDone || response.Page != page || response.PageSize != 100)
                throw new InvalidOperationException("Invalid contact incident query receipt.");
            match = response.Items.SingleOrDefault(item => item.ReportId == contact.AlertCenterReportId &&
                item.Metadata.Value<string>("source") == "CorpHome.ContactSubmission");
            if (match != null || page * 100 >= response.TotalCount) break;
            if (response.Items.Count == 0) throw new InvalidOperationException("Incomplete incident query.");
        }
        if (match == null) throw new InvalidOperationException("The matching contact incident was not found.");
        if (match.Status == "resolved") return;
        var result = await client.ResolveIncidentAsync(match.IncidentId, new ResolveIncidentRequest
        {
            Version = match.Version,
            Reason = "CorpHome confirmed SMTP acceptance of the first response to this inquiry. This closes the first-response task, not proof of customer delivery or final resolution of their request."
        });
        if (result.Code != Code.JobDone) throw new InvalidOperationException("Incident resolution was not confirmed.");
    }

    private sealed class ReplyKeyProvider(string key) : IAlertCenterApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync() => ValueTask.FromResult(key);
    }
}
