using Aiursoft.CorpHome.Entities;

namespace Aiursoft.CorpHome.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required User User { get; set; }
    public required IList<string> Roles { get; set; }
}
