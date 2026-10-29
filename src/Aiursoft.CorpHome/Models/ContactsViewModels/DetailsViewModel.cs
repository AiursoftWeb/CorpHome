using Aiursoft.CorpHome.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ContactsViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public required Contact Contact { get; init; }

    public DetailsViewModel()
    {
        PageTitle = "Contact Details";
    }
}
