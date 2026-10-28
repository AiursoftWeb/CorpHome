using System.Net;
using Aiursoft.CorpHome.Entities;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class ContactsControllerTests : TestBase
{
    [TestMethod]
    public async Task DetailsRequiresLoginAndReturnsToTheSameContactAfterLogin()
    {
        var contact = await AddContactAsync();
        var url = $"/Contacts/Details/{contact.Id}";
        using var challenge = await Http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.Found, challenge.StatusCode);
        Assert.Contains(Uri.EscapeDataString(url), challenge.Headers.Location!.ToString());
        using var login = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            ["EmailOrUserName"] = "admin@default.com", ["Password"] = "Admin@123456!", ["ReturnUrl"] = url
        });
        AssertRedirect(login, url);
        using var response = await Http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("detail-customer@example.invalid", html);
        Assert.Contains("Deployment question", html);
        Assert.DoesNotContain("<script>unsafe</script>", html);
        Assert.Contains("&lt;script&gt;unsafe&lt;/script&gt;", html);
        var list = await Http.GetStringAsync("/Contacts");
        Assert.Contains(url, list);
    }

    [TestMethod]
    public async Task DetailsRequiresContactPermission()
    {
        var contact = await AddContactAsync();
        await RegisterAndLoginAsync();
        using var response = await Http.GetAsync($"/Contacts/Details/{contact.Id}");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual("/Error/Code403", response.Headers.Location!.AbsolutePath);
    }

    [TestMethod]
    public async Task UnknownContactReturnsNotFound()
    {
        await LoginAsAdmin();
        using var response = await Http.GetAsync("/Contacts/Details/2147483647");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Contact> AddContactAsync()
    {
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        var contact = new Contact
        {
            FirstName = "Detail", LastName = "Customer", Company = "<script>unsafe</script>",
            Email = "detail-customer@example.invalid", ServicesProvided = "Deployment question\nSecond line"
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        return contact;
    }
}
