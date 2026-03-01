using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.PermissionsViewModels;

public class PermissionWithRoleCount
{
    public required PermissionDisplayViewModel Permission { get; init; }
    
    [Display(Name = "Role count")]
    public required int RoleCount { get; init; }
    
    [Display(Name = "User count")]
    public required int UserCount { get; init; }
}
