using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Entities;

public class Contact
{
    public int Id { get; set; }
    
    public string OrganizationSize { get; set; } = string.Empty;

    public string ConsumeOpenSource { get; set; } = string.Empty;

    public string ServicesProvided { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool AgreeToInformation { get; set; }

    public bool AgreeToPrivacy { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}