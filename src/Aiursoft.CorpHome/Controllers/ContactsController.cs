using Aiursoft.CorpHome.Authorization;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Models.ContactsViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Controllers;

[Authorize]
[LimitPerMin]
public class ContactsController(TemplateDbContext dbContext) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewContacts)]
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Contacts",
        CascadedLinksIcon = "inbox",
        CascadedLinksOrder = 9997,
        LinkText = "View Contacts",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var contacts = await dbContext.Contacts
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Contacts = contacts
        };

        return this.StackView(model);
    }
}