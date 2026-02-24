using Aiursoft.CorpHome.Configuration;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.FileStorage;

namespace Aiursoft.CorpHome.Views.Shared.Components.MarketingNavbar;

public class MarketingNavbar(
    GlobalSettingsService globalSettingsService,
    StorageService storageService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingNavbarViewModel? model = null)
    {
        model ??= new MarketingNavbarViewModel();
        model.ProjectName = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName);
        var logoPath = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectLogo);
        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            model.LogoUrl = storageService.RelativePathToInternetUrl(logoPath, HttpContext);
        }
        return View(model);
    }
}
