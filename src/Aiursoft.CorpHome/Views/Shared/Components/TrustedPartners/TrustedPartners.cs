using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Views.Shared.Components.TrustedPartners;

public class TrustedPartners(
    GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new TrustedPartnersViewModel
        {
            ShowMoog = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowPartnerMoog) == "True",
            ShowSenparc = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowPartnerSenparc) == "True",
            ShowVoxihost = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowPartnerVoxihost) == "True",
            ShowChopinsight = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowPartnerChopinsight) == "True",
            ShowEgret = await globalSettingsService.GetSettingValueAsync(SettingsMap.ShowPartnerEgret) == "True"
        };

        // If no partner is enabled, return empty content
        if (!model.ShowMoog && !model.ShowSenparc && !model.ShowVoxihost && !model.ShowChopinsight && !model.ShowEgret)
        {
            return Content(string.Empty);
        }

        return View(model);
    }
}
