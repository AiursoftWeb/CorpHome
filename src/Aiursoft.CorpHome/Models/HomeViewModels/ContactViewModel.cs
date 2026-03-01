using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.HomeViewModels;

public class ContactViewModel : UiStackLayoutViewModel
{
    public ContactViewModel()
    {
        PageTitle = "Contact Us";
    }

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Organization Size")]
    public string OrganizationSize { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "How do you consume open source?")]
    public string ConsumeOpenSource { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "What services do you provide?")]
    public string ServicesProvided { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Company")]
    public string Company { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Job Title")]
    public string JobTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [EmailAddress(ErrorMessage = "The {0} is not a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "I agree to receive information about Aiursoft's products and services.")]
    public bool AgreeToInformation { get; set; }

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "By submitting this form, I confirm that I have read and agree to Aiursoft's Terms of Service and Privacy Policy.")]
    public bool AgreeToPrivacy { get; set; }
}