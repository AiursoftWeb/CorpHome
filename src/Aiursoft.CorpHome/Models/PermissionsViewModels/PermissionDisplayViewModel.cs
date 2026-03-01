using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.PermissionsViewModels;

public class PermissionDisplayViewModel
{
    [Display(Name = "Key")]
    public required string Key { get; set; }
    
    [Display(Name = "Name")]
    public required string Name { get; set; }
    
    [Display(Name = "Description")]
    public required string Description { get; set; }
}
