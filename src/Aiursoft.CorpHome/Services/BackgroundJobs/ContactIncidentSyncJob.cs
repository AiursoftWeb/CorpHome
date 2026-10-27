using Aiursoft.AiurProtocol.Models;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Aiursoft.CorpHome.Services.BackgroundJobs;

public sealed class ContactIncidentSyncJob(TemplateDbContext db, AlertCenterSettingsProvider settingsProvider,
    IContactIncidentClient client, ILogger<ContactIncidentSyncJob> logger) : IBackgroundJob
{
    public string Name => "Contact AlertCenter synchronization";
    public string Description => "Reports saved contact submissions as sev2 incidents and retries pending deliveries.";

    public async Task ExecuteAsync()
    {
        var settings = await settingsProvider.GetAsync();
        if (!settings.Enabled) return;
        settings.Validate();

        // 固定本轮上界，避免持续新增的咨询让任务永远无法结束。
        var upperBound = await db.Contacts.MaxAsync(contact => (int?)contact.Id) ?? 0;
        var lastId = 0;
        var failures = 0;
        while (lastId < upperBound)
        {
            var afterId = lastId;
            var contacts = await db.Contacts.Where(contact => contact.Id > afterId && contact.Id <= upperBound &&
                    contact.AlertCenterReportId != null && (contact.AlertCenterReportedAtUtc == null || contact.AlertCenterIncidentId == null))
                .OrderBy(contact => contact.Id).Take(100).ToListAsync();
            if (contacts.Count == 0) break;
            foreach (var contact in contacts)
            {
                lastId = contact.Id;
                try
                {
                    var occurredAt = new DateTimeOffset(DateTime.SpecifyKind(contact.CreateTime, DateTimeKind.Utc));
                    var response = await client.CreateAsync(new CreateIncidentRequest
                    {
                        ReportId = contact.AlertCenterReportId!.Value,
                        Title = $"New contact submission #{contact.Id}",
                        Level = "sev2",
                        OccurredAtUtc = occurredAt,
                        Content = $"A new contact form was saved on the company website.\n\n" +
                            $"Contact ID: {contact.Id}\nSubmitted at (UTC): {occurredAt:O}\n\n" +
                            "Next steps: Open the authorized contact list, review the inquiry and follow up. " +
                            "Contact details remain in CorpHome. Acknowledgement and resolution are manual.\n\n" +
                            $"Contacts: {settings.PublicBaseUrl.TrimEnd('/')}/Contacts#contact-{contact.Id}",
                        Metadata = new JObject { ["source"] = "CorpHome.ContactSubmission", ["contactId"] = contact.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                    });
                    if (response.Code != Code.JobDone || response.ReportId != contact.AlertCenterReportId || response.IncidentId == Guid.Empty)
                        throw new InvalidOperationException("AlertCenter did not return a valid contact receipt.");

                    // 远端成功但本地保存失败时，下轮仍使用同一个 ReportId，由服务端幂等去重。
                    contact.AlertCenterReportedAtUtc ??= DateTime.UtcNow;
                    contact.AlertCenterIncidentId = response.IncidentId;
                    await db.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    failures++;
                    logger.LogError("Contact incident synchronization failed for {ContactId} with {ExceptionType}; retained for retry.",
                        contact.Id, exception.GetType().Name);
                }
                finally
                {
                    db.Entry(contact).State = EntityState.Detached;
                }
            }
        }
        if (failures > 0)
            throw new InvalidOperationException($"{failures} contact incident deliveries remain pending; retry on the next synchronization.");
    }
}
