using System.Security.Claims;
using Aiursoft.CorpHome.Authorization;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Models.ContactsViewModels;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.ContactReplies;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Controllers;

[Authorize]
[LimitPerMin]
public class ContactsController(TemplateDbContext dbContext, ContactReplyService contactReplyService) : Controller
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
            Contacts = contacts.Select(c => new ContactDisplayViewModel
            {
                Id = c.Id,
                OrganizationSize = c.OrganizationSize,
                ConsumeOpenSource = c.ConsumeOpenSource,
                ServicesProvided = c.ServicesProvided,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Company = c.Company,
                JobTitle = c.JobTitle,
                Email = c.Email,
                AgreeToInformation = c.AgreeToInformation,
                AgreeToPrivacy = c.AgreeToPrivacy,
                CreateTime = c.CreateTime
            })
        };

        return this.StackView(model);
    }

    [HttpGet]
    [Authorize(Policy = AppPermissionNames.CanViewContacts)]
    [Authorize(Policy = AppPermissionNames.CanReplyContacts)]
    public async Task<IActionResult> Reply(int id)
    {
        var contact = await dbContext.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        var model = new ReplyViewModel
        {
            Contact = contact,
            Replies = await dbContext.ContactReplies.AsNoTracking()
                .Where(r => r.ContactId == id)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync()
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissionNames.CanViewContacts)]
    [Authorize(Policy = AppPermissionNames.CanReplyContacts)]
    public async Task<IActionResult> Reply(int id, ReplyViewModel model)
    {
        var contact = await dbContext.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        model.Contact = contact;
        model.Replies = await dbContext.ContactReplies.AsNoTracking()
            .Where(r => r.ContactId == id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
        if (model.Replies.Count != 0)
        {
            ModelState.AddModelError(string.Empty, "A reply already exists for this contact. Additional replies and automatic retries are not allowed.");
        }

        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var error = await contactReplyService.QueueAsync(id, userId, model.Subject, model.Body);
        if (error == null)
        {
            return RedirectToAction(nameof(Reply), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        model.Replies = await dbContext.ContactReplies.AsNoTracking()
            .Where(r => r.ContactId == id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
        return this.StackView(model);
    }
}