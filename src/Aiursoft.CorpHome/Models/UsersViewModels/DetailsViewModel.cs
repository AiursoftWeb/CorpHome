using Aiursoft.CorpHome.Authorization;
using Aiursoft.CorpHome.Models.RolesViewModels;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.UsersViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "User Details";
    }

    public required UserDisplayViewModel User { get; set; }

    public required IList<RoleDisplayViewModel> Roles { get; set; }

    public required List<PermissionDescriptor> Permissions { get; set; }
}
