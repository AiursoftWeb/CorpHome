using System.Text.RegularExpressions;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Models.HomeViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.WebTools.Attributes;
using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Controllers;

[LimitPerMin]
public class HomeController(TemplateDbContext dbContext, IStatelessCaptcha captcha, GlobalSettingsService globalSettings) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rawMetas = await globalSettings.GetSettingValueAsync(SettingsMap.CustomMetaTags);
        var model = new IndexViewModel
        {
            Metas = SanitizeMetaTags(rawMetas)
        };
        return this.SimpleView(model);
    }

    private static string? SanitizeMetaTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var safeTags = new List<string>();
        foreach (Match match in MetaTagRegex.Matches(raw))
        {
            var sanitized = SanitizeSingleMeta(match.Value);
            if (sanitized != null)
                safeTags.Add(sanitized);
        }
        return safeTags.Count > 0 ? string.Join("\n", safeTags) : null;
    }

    private static readonly HashSet<string> AllowedMetaAttributes =
        ["name", "property", "content", "charset", "http-equiv"];

    private static readonly HashSet<string> DangerousProtocols =
        ["javascript:", "data:text/html", "vbscript:"];

    private static string? SanitizeSingleMeta(string tag)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match attr in AttributeRegex.Matches(tag))
        {
            var key = attr.Groups[1].Value.ToLowerInvariant();
            var value = attr.Groups[2].Value;

            if (!AllowedMetaAttributes.Contains(key)) return null;
            if (DangerousProtocols.Any(p => value.ToLowerInvariant().Contains(p))) return null;

            attrs[key] = value;
        }

        if (!attrs.ContainsKey("content") && !attrs.ContainsKey("charset")) return null;

        var parts = attrs.Select(kv => $"{kv.Key}=\"{System.Net.WebUtility.HtmlEncode(kv.Value)}\"");
        return $"<meta {string.Join(" ", parts)} />";
    }

    private static readonly Regex MetaTagRegex =
        new(@"<meta\b[^>]*/>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttributeRegex =
        new(@"(\w[\w-]*)\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IActionResult Substratum()
    {
        return this.SimpleView(new SubstratumViewModel());
    }

    [Route("/know-us")]
    [Route("/know-us.html")]
    public IActionResult KnowUs()
    {
        return this.SimpleView(new KnowUsViewModel());
    }

    [Route("/anduinos")]
    [Route("/anduinos.html")]
    public IActionResult Anduinos()
    {
        return this.SimpleView(new AnduinosViewModel());
    }

    [Route("/terms")]
    [Route("/terms.html")]
    public IActionResult Terms()
    {
        return this.SimpleView(new TermsViewModel());
    }

    [Route("/privacy")]
    [Route("/privacy.html")]
    public IActionResult Privacy()
    {
        return this.SimpleView(new PrivacyViewModel());
    }

    public IActionResult Contact()
    {
        return this.SimpleView(new ContactViewModel());
    }

    [Route("get-captcha-image")]
    public IActionResult GetCaptchaImage()
    {
        var result = captcha.GenerateCaptcha();
        return Json(new
        {
            token = result.Token,
            imageBase64 = Convert.ToBase64String(result.ImageBytes)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!captcha.Validate(model.CaptchaCode, model.CaptchaToken))
        {
            ModelState.AddModelError(nameof(model.CaptchaCode), "The verification code is incorrect. Please try again.");
        }

        if (!model.AgreeToPrivacy)
        {
            ModelState.AddModelError(nameof(model.AgreeToPrivacy), "You must agree to the Terms of Service and Privacy Policy to submit this form.");
        }

        if (!ModelState.IsValid)
        {
            return this.SimpleView(model);
        }

        var contact = new Contact
        {
            OrganizationSize = model.OrganizationSize,
            ConsumeOpenSource = model.ConsumeOpenSource,
            ServicesProvided = model.ServicesProvided,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Company = model.Company,
            JobTitle = model.JobTitle,
            Email = model.Email,
            AgreeToInformation = model.AgreeToInformation,
            AgreeToPrivacy = model.AgreeToPrivacy
        };

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(ContactSuccess));
    }

    public IActionResult ContactSuccess()
    {
        return this.SimpleView(new ContactSuccessViewModel());
    }
}
