using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Aiursoft.CorpHome.Services.ContactAlerts;

public sealed record FollowUpEmail(string MessageId, DateTime SentAtUtc, DateTime ReceivedAtUtc,
    string[] From, string[] Senders, string[] Recipients, bool IsAutomatic);

public sealed record ContactMailboxPage(long UidValidity, long LastUid, IReadOnlyList<FollowUpEmail> Emails);

public interface IContactMailbox
{
    IAsyncEnumerable<ContactMailboxPage> ReadAsync(ContactMailSettings settings, ContactMailboxCheckpoint checkpoint, DateTime sinceUtc);
}

public sealed class ImapContactMailbox : IContactMailbox
{
    public async IAsyncEnumerable<ContactMailboxPage> ReadAsync(ContactMailSettings settings,
        ContactMailboxCheckpoint checkpoint, DateTime sinceUtc)
    {
        using var client = new ImapClient();
        client.Timeout = 30000;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var token = timeout.Token;
        await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.SslOnConnect, token);
        await client.AuthenticateAsync(settings.Username, settings.Password, token);
        var folder = await client.GetFolderAsync(settings.Folder, token);
        await folder.OpenAsync(FolderAccess.ReadOnly, token);
        var lastUid = checkpoint.UidValidity == folder.UidValidity ? checkpoint.LastUid : 0;
        // IMAP 按日期而非时刻检索，多取一天避免服务器时区差异漏信。
        SearchQuery query = SearchQuery.DeliveredAfter(sinceUtc.Date.AddDays(-1));
        if (lastUid > 0)
            query = query.And(SearchQuery.Uids(new UniqueIdRange(new UniqueId((uint)lastUid + 1), UniqueId.MaxValue)));
        var uids = await folder.SearchAsync(query, token);
        // 每轮最多取 1000 封的信头；保存进度后由下轮继续，避免长期占用后台任务。
        foreach (var batch in uids.Where(uid => uid.Id > lastUid).OrderBy(uid => uid.Id).Take(1000).Chunk(100))
        {
            var summaries = await folder.FetchAsync(batch, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate |
                MessageSummaryItems.UniqueId, new[] { HeaderId.AutoSubmitted }, token);
            var emails = new List<FollowUpEmail>();
            foreach (var summary in summaries.Where(item => batch.Contains(item.UniqueId)))
            {
                var envelope = summary.Envelope;
                if (envelope?.Date == null || summary.InternalDate == null) continue;
                var automatic = summary.Headers?[HeaderId.AutoSubmitted];
                emails.Add(new FollowUpEmail($"{folder.UidValidity}:{summary.UniqueId.Id}",
                    envelope.Date.Value.UtcDateTime, summary.InternalDate.Value.UtcDateTime,
                    envelope.From.Mailboxes.Select(item => item.Address).ToArray(),
                    envelope.Sender.Mailboxes.Select(item => item.Address).ToArray(),
                    envelope.To.Mailboxes.Concat(envelope.Cc.Mailboxes).Select(item => item.Address).ToArray(),
                    !string.IsNullOrWhiteSpace(automatic) && !automatic.Trim().Equals("no", StringComparison.OrdinalIgnoreCase)));
            }
            yield return new ContactMailboxPage(folder.UidValidity, batch[^1].Id, emails);
        }
        await client.DisconnectAsync(true, token);
    }
}
