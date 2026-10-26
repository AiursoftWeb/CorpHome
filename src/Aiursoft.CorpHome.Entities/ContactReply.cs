using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Entities;

public class ContactReply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    [MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [MaxLength(10000)] public string Body { get; set; } = string.Empty;
    [MaxLength(320)] public string Recipient { get; set; } = string.Empty;
    [MaxLength(320)] public string ReplyTo { get; set; } = string.Empty;
    [MaxLength(320)] public string Sender { get; set; } = string.Empty;
    [MaxLength(450)] public string AuthorId { get; set; } = string.Empty;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AttemptedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? IncidentResolvedAtUtc { get; set; }
    [MaxLength(1000)] public string? Error { get; set; }
}
