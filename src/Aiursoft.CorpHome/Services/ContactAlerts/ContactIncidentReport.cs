using System.Globalization;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Newtonsoft.Json.Linq;

namespace Aiursoft.CorpHome.Services.ContactAlerts;

public static class ContactIncidentReport
{
    public static CreateIncidentRequest Create(Contact contact, AlertCenterSettings settings, ContactMailSettings followUp)
    {
        var occurredAt = new DateTimeOffset(DateTime.SpecifyKind(contact.CreateTime, DateTimeKind.Utc));
        var url = $"{settings.PublicBaseUrl.TrimEnd('/')}/Contacts/Details/{contact.Id}";
        var name = string.Join(" ", new[] { contact.FirstName, contact.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var titleName = SingleLine(name, 80);
        var titleCompany = SingleLine(contact.Company, 80);
        var title = "【CorpHome】New Consumer";
        if (titleName.Length > 0) title += $" {titleName}";
        if (titleCompany.Length > 0) title += $" From {titleCompany}";
        var customerEmail = SingleLine(contact.Email, 320);
        var instructions = followUp.Enabled
            ? $"Send a new email from your company email account.\nTo: {customerEmail}\nCC: {SingleLine(followUp.Username, 320)}\n\n" +
              $"Reply within {followUp.FollowUpDays} days of this inquiry. " +
              "Your reply will automatically mark this alert as mitigated on the next mailbox check."
            : $"Send a new email to {customerEmail}.\n" +
              "Automatic email follow-up detection is not enabled; update this alert manually after replying.";
        var content = new List<string> { "Customer", "" };
        AddField(content, "Name", name, 120);
        AddField(content, "Company", contact.Company, 160);
        AddField(content, "Job title", contact.JobTitle, 120);
        AddField(content, "Email", contact.Email, 320);
        AddField(content, "Organization size", contact.OrganizationSize, 80);
        AddField(content, "Open-source usage", contact.ConsumeOpenSource, 300);
        var prefix = string.Join('\n', content) + "\n\nServices / inquiry\n";
        var footer = $"\n\nHow to reply\n{instructions}\n\nView full inquiry: {url}";
        var inquiry = Shorten(contact.ServicesProvided, Math.Min(1500, 4000 - prefix.Length - footer.Length));

        return new CreateIncidentRequest
        {
            ReportId = contact.AlertCenterReportId!.Value,
            Title = title,
            Level = "sev3",
            OccurredAtUtc = occurredAt,
            Content = prefix + inquiry + footer,
            Context = new JObject
            {
                ["source"] = "CorpHome.ContactSubmission",
                ["contactId"] = contact.Id,
                ["submittedAtUtc"] = occurredAt,
                ["detailsUrl"] = url,
                ["customer"] = new JObject
                {
                    ["firstName"] = Shorten(contact.FirstName, 200),
                    ["lastName"] = Shorten(contact.LastName, 200),
                    ["email"] = Shorten(contact.Email, 320),
                    ["company"] = Shorten(contact.Company, 300),
                    ["jobTitle"] = Shorten(contact.JobTitle, 200),
                    ["organizationSize"] = Shorten(contact.OrganizationSize, 100)
                },
                ["consumeOpenSource"] = Shorten(contact.ConsumeOpenSource, 500),
                ["servicesProvided"] = Shorten(contact.ServicesProvided, 3000),
                ["consent"] = new JObject
                {
                    ["receiveInformation"] = contact.AgreeToInformation,
                    ["privacy"] = contact.AgreeToPrivacy
                },
                ["followUp"] = new JObject
                {
                    ["automaticMitigationEnabled"] = followUp.Enabled,
                    ["windowDays"] = followUp.FollowUpDays
                }
            },
            Metadata = new JObject
            {
                ["source"] = "CorpHome.ContactSubmission",
                ["contactId"] = contact.Id.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private static void AddField(List<string> lines, string label, string value, int length)
    {
        if (!string.IsNullOrWhiteSpace(value)) lines.Add($"{label}: {SingleLine(value, length)}");
    }

    private static string SingleLine(string value, int length) =>
        Shorten(string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), length);

    private static string Shorten(string value, int length)
    {
        if (value.Length <= length) return value;
        var end = length - 1;
        if (char.IsHighSurrogate(value[end - 1])) end--;
        return value[..end] + "…";
    }
}
