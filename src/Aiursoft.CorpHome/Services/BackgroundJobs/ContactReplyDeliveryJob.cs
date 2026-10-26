using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services.ContactReplies;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Services.BackgroundJobs;

public sealed class ContactReplyDeliveryJob(TemplateDbContext db, IContactEmailSender sender,
    IContactIncidentResolver resolver, ILogger<ContactReplyDeliveryJob> logger) : IBackgroundJob
{
    public string Name => "Contact reply delivery";
    public string Description => "Sends queued replies once and retries incident resolution without resending email.";

    public async Task ExecuteAsync()
    {
        var ids = await db.ContactReplies.AsNoTracking().Where(reply => reply.Status == "pending" ||
                reply.Status == "sent" && reply.IncidentResolvedAtUtc == null)
            .OrderBy(reply => reply.CreatedAtUtc).Select(reply => reply.Id).ToListAsync();
        var failures = 0;
        foreach (var id in ids)
        {
            try
            {
                var reply = await db.ContactReplies.Include(item => item.Contact).SingleAsync(item => item.Id == id);
                if (reply.Status == "pending")
                {
                    // 发信前原子认领。进程中断遗留的 sending 结果未知，绝不自动重发。
                    var claimed = await db.ContactReplies.Where(item => item.Id == id && item.Status == "pending")
                        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "sending")
                            .SetProperty(item => item.AttemptedAtUtc, DateTime.UtcNow));
                    if (claimed == 0) continue;
                    await db.Entry(reply).ReloadAsync();
                    try
                    {
                        await sender.SendAsync(reply);
                    }
                    catch (Exception exception)
                    {
                        reply.Status = "unknown";
                        reply.Error = "Email delivery is unconfirmed. Check the SMTP logs before taking any further action; automatic resend and closure are disabled.";
                        await db.SaveChangesAsync();
                        logger.LogError("Contact reply {ReplyId} delivery is unconfirmed ({ExceptionType}).", id, exception.GetType().Name);
                        failures++;
                        continue;
                    }
                    reply.Status = "sent";
                    reply.SentAtUtc = DateTime.UtcNow;
                    reply.Error = null;
                    await db.SaveChangesAsync();
                }
                if (reply.Status != "sent" || reply.IncidentResolvedAtUtc != null) continue;
                await resolver.ResolveAsync(reply.Contact);
                reply.IncidentResolvedAtUtc = DateTime.UtcNow;
                reply.Error = null;
                await db.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                failures++;
                logger.LogError("Contact reply {ReplyId} processing failed ({ExceptionType}); inspect its persisted delivery status.", id, exception.GetType().Name);
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }
        if (failures > 0) throw new InvalidOperationException($"{failures} contact replies require delivery review or incident-resolution retry.");
    }
}
