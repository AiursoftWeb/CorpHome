using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ManageViewModels;

public class IndexViewModel: UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Manage";
    }

    [Display(Name = "Allow user adjust nickname")]
    public bool AllowUserAdjustNickname { get; set; }
}
