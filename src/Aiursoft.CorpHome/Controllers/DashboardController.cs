using Aiursoft.CorpHome.Models.DashboardViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Controllers;

[LimitPerMin]
public class DashboardController : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Index",
        LinkOrder = 1)]
    public IActionResult Index()
    {
        return this.StackView(new IndexViewModel());
    }
}
