using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Models.HomeViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.WebTools.Attributes;
using Edi.Captcha;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Controllers;

[LimitPerMin]
public class HomeController(TemplateDbContext dbContext, IStatelessCaptcha captcha) : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }

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
        var result = captcha.GenerateCaptcha(100, 36);
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
