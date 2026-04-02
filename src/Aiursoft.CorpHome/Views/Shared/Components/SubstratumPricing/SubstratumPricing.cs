using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Views.Shared.Components.SubstratumPricing;

public class SubstratumPricing : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        SubstratumPricingViewModel model;

        if (culture.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-Hant-HK", StringComparison.OrdinalIgnoreCase))
        {
            model = new SubstratumPricingViewModel
            {
                PriceStarter = "HK$18",
                PriceStartup = "HK$28",
                PriceCustom = "HK$48",
                SetupFeeEstimate = "HK$3000"
            };
        }
        else if (culture.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                 culture.Equals("zh-Hans-CN", StringComparison.OrdinalIgnoreCase))
        {
            model = new SubstratumPricingViewModel
            {
                PriceStarter = "¥15",
                PriceStartup = "¥23",
                PriceCustom = "¥39",
                SetupFeeEstimate = "¥2000"
            };
        }
        else
        {
            model = new SubstratumPricingViewModel
            {
                PriceStarter = "$2.49",
                PriceStartup = "$3.99",
                PriceCustom = "$6.49",
                SetupFeeEstimate = "$349"
            };
        }

        return View(model);
    }
}
