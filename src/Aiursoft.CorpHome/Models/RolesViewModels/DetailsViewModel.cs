using Aiursoft.CorpHome.Authorization;
using Aiursoft.CorpHome.Models.UsersViewModels;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.RolesViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Role Details";
    }

    public required RoleDisplayViewModel Role { get; set; }

    public required List<PermissionDescriptor> Permissions { get; set; }

    public required IList<UserDisplayViewModel> UsersInRole { get; set; }
}
