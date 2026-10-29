using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Entities;

public class ContactMailboxCheckpoint
{
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    public long UidValidity { get; set; }

    public long LastUid { get; set; }
}
