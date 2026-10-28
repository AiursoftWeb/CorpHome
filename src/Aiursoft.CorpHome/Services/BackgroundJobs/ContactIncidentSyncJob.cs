using Aiursoft.AiurProtocol.Models;
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Services.BackgroundJobs;

public sealed class ContactIncidentSyncJob(TemplateDbContext db, AlertCenterSettingsProvider settingsProvider,
    IContactIncidentClient client, ILogger<ContactIncidentSyncJob> logger, ContactMailSettingsProvider mailSettingsProvider) : IBackgroundJob
{
    public string Name => "Contact AlertCenter synchronization";
    public string Description => "Reports saved contact submissions as sev2 incidents and retries pending deliveries.";

    public async Task ExecuteAsync()
    {
        var settings = await settingsProvider.GetAsync();
        if (!settings.Enabled) return;
        settings.Validate();
        var followUp = await mailSettingsProvider.GetAsync();

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
                    var response = await client.CreateAsync(ContactIncidentReport.Create(contact, settings, followUp));
                    if (response.Code is not (Code.JobDone or Code.NoActionTaken) || response.ReportId != contact.AlertCenterReportId || response.IncidentId == Guid.Empty)
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
