using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Views.Shared.Components.VoxihostPartnership;

public class VoxihostPartnership(
    GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var showPartnership = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowVoxihostPartnership);
        if (showPartnership != "True")
        {
            return Content(string.Empty);
        }
        return View();
    }
}
