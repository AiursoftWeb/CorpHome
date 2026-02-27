using Aiursoft.CorpHome.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ContactsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "View Contacts";
    }

    public IEnumerable<Contact> Contacts { get; set; } = [];
}