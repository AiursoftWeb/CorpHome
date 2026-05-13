using System.Net;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class SystemControllerTests : TestBase
{
    [TestMethod]
    public async Task TestIndex()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/System/Index");
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task TestIndexContainsTableCounts()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/System/Index");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(html.Contains("Database Table Counts"), "Should contain table counts section");
        Assert.IsTrue(html.Contains("Users") || html.Contains("User"), "Should contain Users table");
    }

    [TestMethod]
    public async Task TestIndexContainsMigrationInfo()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/System/Index");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(html.Contains("Database Migrations"), "Should contain migrations section");
        Assert.IsTrue(html.Contains("Applied / Defined"), "Should contain applied/defined summary");
    }

    [TestMethod]
    public async Task TestShutdown()
    {
        await LoginAsAdmin();
        var response = await Http.PostAsync("/System/Shutdown", null);
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    }
}
