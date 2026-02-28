using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Models.HomeViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.CorpHome.Controllers;

[LimitPerMin]
public class HomeController(TemplateDbContext dbContext) : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }

    public IActionResult Contact()
    {
        return this.SimpleView(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!model.AgreeToPrivacy)
        {
            ModelState.AddModelError(nameof(model.AgreeToPrivacy), "You must agree to the Privacy Policy to submit this form.");
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
