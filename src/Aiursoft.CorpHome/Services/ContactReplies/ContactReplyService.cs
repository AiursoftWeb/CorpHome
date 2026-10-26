using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aiursoft.CorpHome.Services.ContactReplies;

public sealed class ContactReplyService(TemplateDbContext db, IOptions<ContactEmailSettings> email)
{
    public async Task<string?> QueueAsync(int contactId, string userId, string subject, string body)
    {
        var settings = email.Value;
        if (!settings.IsConfigured()) return "Contact email sending is not configured.";
        if (string.IsNullOrWhiteSpace(userId) || !settings.EngineerAddresses.TryGetValue(userId, out var replyTo) ||
            !ContactEmailSettings.IsAddress(replyTo))
            return "An administrator must configure your trusted company reply address before you can send email.";
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 200 || subject.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(body) || body.Length > 10000)
            return "Enter a subject of at most 200 characters and a message of at most 10,000 characters.";
        var contact = await db.Contacts.SingleOrDefaultAsync(item => item.Id == contactId);
        if (contact == null) return "The contact no longer exists.";
        if (!ContactEmailSettings.IsAddress(contact.Email)) return "The contact email address is invalid.";
        if (await db.ContactReplies.AnyAsync(reply => reply.ContactId == contactId))
            return "A reply has already been queued for this contact. Check its delivery status before taking further action.";
        var reply = new ContactReply
        {
            ContactId = contactId, Subject = subject.Trim(), Body = body.Trim(),
            Recipient = contact.Email, ReplyTo = replyTo, Sender = settings.From, AuthorId = userId
        };
        db.ContactReplies.Add(reply);
        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException)
        {
            db.Entry(reply).State = EntityState.Detached;
            if (await db.ContactReplies.AnyAsync(item => item.ContactId == contactId))
                return "Another engineer has already queued a reply for this contact.";
            throw;
        }
    }
}
