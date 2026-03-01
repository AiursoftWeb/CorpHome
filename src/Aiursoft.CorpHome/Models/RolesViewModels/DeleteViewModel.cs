using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.RolesViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete Role";
    }

    public required RoleDisplayViewModel Role { get; set; }
}
