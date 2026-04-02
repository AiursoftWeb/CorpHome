using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Views.Shared.Components.SubstratumHero;

public class SubstratumHero : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
