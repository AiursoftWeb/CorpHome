namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class HomeControllerTests : TestBase
{
    [TestMethod]
    public async Task GetIndex()
    {
        var url = "/";
        var response = await Http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetChopInsightCaseStudy()
    {
        var response = await Http.GetAsync("/case-study/chopinsight");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("ChopInsight builds an AI-ready research platform", html);
        Assert.Contains("The challenge", html);
        Assert.Contains("The solution", html);
        Assert.Contains("The results", html);
    }
}
