using System.ComponentModel.DataAnnotations;

namespace Aiursoft.CorpHome.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required UserDisplayViewModel User { get; set; }
    public required IList<string> Roles { get; set; }
}

public class UserDisplayViewModel
{
    [Display(Name = "ID")]
    public required string Id { get; set; }
    
    [Display(Name = "User name")]
    public required string UserName { get; set; }
    
    [Display(Name = "Name")]
    public required string DisplayName { get; set; }
    
    [Display(Name = "Email")]
    public required string Email { get; set; }
    
    [Display(Name = "Avatar relative path")]
    public required string AvatarRelativePath { get; set; }
    
    [Display(Name = "Creation time")]
    public DateTime CreationTime { get; set; }
}
