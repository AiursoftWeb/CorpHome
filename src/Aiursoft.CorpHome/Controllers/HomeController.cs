using Aiursoft.CorpHome.Models.HomeViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Controllers;

[LimitPerMin]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }
}
