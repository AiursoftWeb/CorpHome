using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.HomeViewModels;

public class ContactViewModel : UiStackLayoutViewModel
{
    public ContactViewModel()
    {
        PageTitle = "Contact Us";
    }

    [Required]
    [Display(Name = "Organization Size")]
    public string OrganizationSize { get; set; } = string.Empty;

    [Required]
    [Display(Name = "How do you consume open source?")]
    public string ConsumeOpenSource { get; set; } = string.Empty;

    [Required]
    [Display(Name = "What services do you provide?")]
    public string ServicesProvided { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Company")]
    public string Company { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Job Title")]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "I agree to receive information about Aiursoft's products and services.")]
    public bool AgreeToInformation { get; set; }

    [Required]
    [Display(Name = "By submitting this form, I confirm that I have read and agree to Canonical's Privacy Notice and Privacy Policy.")]
    public bool AgreeToPrivacy { get; set; }
}