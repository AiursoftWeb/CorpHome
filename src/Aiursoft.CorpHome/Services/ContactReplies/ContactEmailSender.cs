using System.Net;
using System.Net.Mail;
using System.Text;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Microsoft.Extensions.Options;

namespace Aiursoft.CorpHome.Services.ContactReplies;

public interface IContactEmailSender
{
    Task SendAsync(ContactReply reply);
}

public sealed class ContactEmailSender(IOptions<ContactEmailSettings> options) : IContactEmailSender
{
    public async Task SendAsync(ContactReply reply)
    {
        var settings = options.Value;
        if (!settings.IsConfigured() || !ContactEmailSettings.IsAddress(reply.Recipient) ||
            !ContactEmailSettings.IsAddress(reply.ReplyTo) || !ContactEmailSettings.IsAddress(reply.Sender))
            throw new InvalidOperationException("Invalid contact SMTP configuration or recipient.");
        using var message = new MailMessage();
        message.From = new MailAddress(reply.Sender);
        message.Subject = reply.Subject;
        message.Body = reply.Body;
        message.SubjectEncoding = Encoding.UTF8;
        message.BodyEncoding = Encoding.UTF8;
        message.IsBodyHtml = false;
        message.To.Add(reply.Recipient);
        message.ReplyToList.Add(reply.ReplyTo);
        message.Headers.Add("Message-ID", $"<corphome.contact.{reply.Id:N}@{message.From.Host.ToLowerInvariant()}>");
        using var client = new SmtpClient(settings.Host, settings.Port);
        client.EnableSsl = settings.RequireTls;
        client.UseDefaultCredentials = false;
        client.Timeout = 10000;
        if (!string.IsNullOrWhiteSpace(settings.UserName))
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await client.SendMailAsync(message, timeout.Token);
    }
}
