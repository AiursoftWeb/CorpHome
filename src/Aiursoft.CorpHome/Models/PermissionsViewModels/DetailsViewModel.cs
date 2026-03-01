using Aiursoft.CorpHome.Models.RolesViewModels;
using Aiursoft.CorpHome.Models.UsersViewModels;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.PermissionsViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Permission Details";
    }

    public required PermissionDisplayViewModel Permission { get; set; }

    public required List<RoleDisplayViewModel> Roles { get; set; }

    public required List<UserDisplayViewModel> Users { get; set; }
}
