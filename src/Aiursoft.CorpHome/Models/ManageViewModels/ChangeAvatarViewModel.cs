using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ManageViewModels;

public class ChangeAvatarViewModel : UiStackLayoutViewModel
{
    public ChangeAvatarViewModel()
    {
        PageTitle = "Change Avatar";
    }

    [NotNull]
    [Display(Name = "Avatar file")]
    [Required(ErrorMessage = "The avatar file is required.")]
    [RegularExpression(@"^avatar.*", ErrorMessage = "The avatar file is invalid. Please upload it again.")]
    [MaxLength(512, ErrorMessage = "The {0} must be less than {1} characters.")]
    [MinLength(2, ErrorMessage = "The {0} must be more than {1} characters.")]
    public string? AvatarUrl { get; set; }
}
