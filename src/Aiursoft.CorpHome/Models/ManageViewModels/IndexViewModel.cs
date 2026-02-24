using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ManageViewModels;

public class IndexViewModel: UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Manage";
    }

    public bool AllowUserAdjustNickname { get; set; }
}
