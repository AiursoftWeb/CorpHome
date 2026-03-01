using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ContactsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "View Contacts";
    }

    public IEnumerable<ContactDisplayViewModel> Contacts { get; set; } = [];
}

public class ContactDisplayViewModel
{
    [Display(Name = "ID")]
    public int Id { get; set; }
    
    [Display(Name = "Organization size")]
    public required string OrganizationSize { get; set; }

    [Display(Name = "Consume open source")]
    public required string ConsumeOpenSource { get; set; }

    [Display(Name = "Services provided")]
    public required string ServicesProvided { get; set; }

    [Display(Name = "First name")]
    public required string FirstName { get; set; }

    [Display(Name = "Last name")]
    public required string LastName { get; set; }

    [Display(Name = "Company")]
    public required string Company { get; set; }

    [Display(Name = "Job title")]
    public required string JobTitle { get; set; }

    [Display(Name = "Email")]
    public required string Email { get; set; }

    [Display(Name = "Agree to information")]
    public bool AgreeToInformation { get; set; }

    [Display(Name = "Agree to privacy")]
    public bool AgreeToPrivacy { get; set; }

    [Display(Name = "Create time")]
    public DateTime CreateTime { get; set; }
}