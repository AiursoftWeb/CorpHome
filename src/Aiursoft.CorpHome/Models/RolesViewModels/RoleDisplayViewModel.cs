using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.RolesViewModels;

public class RoleDisplayViewModel
{
    [Display(Name = "ID")]
    public required string Id { get; set; }
    
    [Display(Name = "Role name")]
    public required string Name { get; set; }
}
