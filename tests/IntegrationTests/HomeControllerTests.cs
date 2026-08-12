using System.Net;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class HomeControllerTests : TestBase
{
    [TestInitialize]
    public override async Task CreateServer()
    {
        const string applicationNameVariable = "ASPNETCORE_APPLICATIONNAME";
        var previousApplicationName = Environment.GetEnvironmentVariable(applicationNameVariable);
        Environment.SetEnvironmentVariable(applicationNameVariable, typeof(Aiursoft.CorpHome.Startup).Assembly.GetName().Name);

        try
        {
            await base.CreateServer();
        }
        finally
        {
            Environment.SetEnvironmentVariable(applicationNameVariable, previousApplicationName);
        }
    }

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

    [TestMethod]
    public async Task GetChopInsightCaseStudyInSimplifiedChinese()
    {
        await Http.GetAsync("/Culture/Set?culture=zh-CN&returnUrl=/case-study/chopinsight");

        var response = await Http.GetAsync("/case-study/chopinsight");
        response.EnsureSuccessStatusCode();

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("上海葱花投研智能科技有限公司", html);
        Assert.DoesNotContain("上海卓创智能科技有限公司", html);
    }
}
