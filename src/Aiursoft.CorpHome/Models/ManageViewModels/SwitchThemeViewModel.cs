using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
