using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.RolesViewModels;

public class IdentityRoleWithCount
{
    public required RoleDisplayViewModel Role { get; init; }
    
    [Display(Name = "User count")]
    public required int UserCount { get; init; }
    
    [Display(Name = "Permission count")]
    public required int PermissionCount { get; init; }
    
    [Display(Name = "Permission names")]
    public required List<string> PermissionNames { get; init; }
}
