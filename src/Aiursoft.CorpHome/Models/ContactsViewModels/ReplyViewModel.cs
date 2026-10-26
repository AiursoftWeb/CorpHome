using System.ComponentModel.DataAnnotations;
using Aiursoft.CorpHome.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aiursoft.CorpHome.Models.ContactsViewModels;

public class ReplyViewModel : UiStackLayoutViewModel
{
    public ReplyViewModel()
    {
        PageTitle = "Reply to Contact";
    }

    [BindNever]
    [ValidateNever]
    public Contact Contact { get; set; } = null!;

    [Required]
    [StringLength(200)]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = "Re: Your inquiry to Aiursoft";

    [Required]
    [StringLength(10000)]
    [Display(Name = "Body")]
    public string Body { get; set; } = string.Empty;

    [BindNever]
    [ValidateNever]
    public List<ContactReply> Replies { get; set; } = [];
}
