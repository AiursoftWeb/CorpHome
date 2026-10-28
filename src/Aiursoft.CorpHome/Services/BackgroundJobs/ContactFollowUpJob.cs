using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Services.BackgroundJobs;

public sealed class ContactFollowUpJob(TemplateDbContext db, ContactMailSettingsProvider settingsProvider,
    AlertCenterSettingsProvider alertSettingsProvider, IContactMailbox mailbox, IContactIncidentClient incidents,
    ILogger<ContactFollowUpJob> logger) : IBackgroundJob
{
    public string Name => "Contact email follow-up";
    public string Description => "Detects staff follow-up emails and mitigates matching contact incidents.";

    public async Task ExecuteAsync()
    {
        var settings = await settingsProvider.GetAsync();
        if (!settings.Enabled || !(await alertSettingsProvider.GetAsync()).Enabled) return;
        settings.Validate();
        var failures = 0;
        try
        {
            await ReadFollowUpsAsync(settings);
        }
        catch (Exception exception)
        {
            failures++;
            logger.LogError("Contact mailbox synchronization failed with {ExceptionType}; saved progress retained for retry.", exception.GetType().Name);
            // 收信失败不能阻止此前已保存的跟进证据继续缓解告警。
            db.ChangeTracker.Clear();
        }

        var lastId = 0;
        while (true)
        {
            var afterId = lastId;
            var contacts = await db.Contacts.Where(contact => contact.Id > afterId && contact.FollowUpReceivedAtUtc != null &&
                    contact.AlertCenterIncidentId != null && contact.AlertCenterMitigatedAtUtc == null)
                .OrderBy(contact => contact.Id).Take(100).ToListAsync();
            if (contacts.Count == 0) break;
            foreach (var contact in contacts)
            {
                lastId = contact.Id;
                try
                {
                    await incidents.MitigateAsync(contact.AlertCenterIncidentId!.Value);
                    contact.AlertCenterMitigatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    failures++;
                    logger.LogError("Contact mitigation failed for {ContactId} with {ExceptionType}; retained for retry.", contact.Id, exception.GetType().Name);
                }
                finally
                {
                    db.Entry(contact).State = EntityState.Detached;
                }
            }
        }
        if (failures > 0)
            throw new InvalidOperationException($"{failures} contact follow-up operations failed; retry on the next synchronization.");
    }

    private async Task ReadFollowUpsAsync(ContactMailSettings settings)
    {
        var now = DateTime.UtcNow;
        var since = now.AddDays(-settings.FollowUpDays);
        var checkpoint = await db.ContactMailboxCheckpoints.FindAsync(settings.MailboxId);
        if (checkpoint == null)
        {
            checkpoint = new ContactMailboxCheckpoint { Id = settings.MailboxId };
            db.ContactMailboxCheckpoints.Add(checkpoint);
        }
        await foreach (var page in mailbox.ReadAsync(settings, checkpoint, since))
        {
            var observedAt = DateTime.UtcNow;
            var emails = page.Emails.Where(email => !email.IsAutomatic && email.From.Length == 1 &&
                settings.AllowsSender(email.From[0]) && email.Senders.All(settings.AllowsSender) &&
                email.SentAtUtc >= since && email.SentAtUtc <= observedAt && email.ReceivedAtUtc <= observedAt).ToArray();
            var recipients = emails.SelectMany(email => email.Recipients).Where(address => !settings.AllowsSender(address))
                .Select(address => address.Trim().ToLowerInvariant()).Distinct().ToArray();
            if (recipients.Length > 0)
            {
                var oldestSubmission = emails.Min(email => email.SentAtUtc).AddDays(-settings.FollowUpDays);
                var newestSubmission = emails.Max(email => email.SentAtUtc);
                var contacts = await db.Contacts.Where(contact => contact.AlertCenterReportId != null &&
                    contact.FollowUpReceivedAtUtc == null && contact.CreateTime >= oldestSubmission && contact.CreateTime <= newestSubmission &&
                    recipients.Contains(contact.Email.Trim().ToLower())).ToListAsync();
                foreach (var contact in contacts)
                {
                    var email = emails.FirstOrDefault(email => email.SentAtUtc >= contact.CreateTime &&
                        email.ReceivedAtUtc >= contact.CreateTime && email.SentAtUtc <= contact.CreateTime.AddDays(settings.FollowUpDays) &&
                        email.Recipients.Contains(contact.Email.Trim(), StringComparer.OrdinalIgnoreCase));
                    if (email == null) continue;
                    contact.FollowUpReceivedAtUtc = email.ReceivedAtUtc;
                    contact.FollowUpMessageId = $"{settings.MailboxId}:{email.MessageId}";
                }
            }
            checkpoint.UidValidity = page.UidValidity;
            checkpoint.LastUid = page.LastUid;
            // 跟进证据和 UID 进度在同次提交中保存，保存失败时不越过未处理邮件。
            await db.SaveChangesAsync();
            foreach (var entry in db.ChangeTracker.Entries<Contact>().ToArray()) entry.State = EntityState.Detached;
        }
        db.Entry(checkpoint).State = EntityState.Detached;
    }
}
