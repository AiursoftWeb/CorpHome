using System.Net;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class CultureControllerTests : TestBase
{
    [TestMethod]
    public async Task SetCulture()
    {
        var url = "/Culture/Set?culture=en&returnUrl=/";
        var response = await Http.GetAsync(url);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
    }

    [TestMethod]
    public async Task SetCultureEmpty()
    {
        var url = "/Culture/Set?culture=&returnUrl=/";
        var response = await Http.GetAsync(url);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task SetCultureNonLocalReturn()
    {
        var url = "/Culture/Set?culture=en&returnUrl=https://google.com";
        var response = await Http.GetAsync(url);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual("/", response.Headers.Location?.OriginalString);
    }

    [TestMethod]
    [DataRow("zh-CN")]
    public async Task SimplifiedChineseUsesSuzhouCompanyBrand(string culture)
    {
        var cultureResponse = await Http.GetAsync($"/Culture/Set?culture={culture}&returnUrl=/");
        Assert.AreEqual(HttpStatusCode.Found, cultureResponse.StatusCode);

        var response = await Http.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("苏州艾软科技有限公司", html);
    }

    [TestMethod]
    [DataRow("en-GB")]
    [DataRow("de-DE")]
    [DataRow("zh-HK")]
    [DataRow("zh-TW")]
    public async Task NonChineseCulturesUseInternationalBrand(string culture)
    {
        var cultureResponse = await Http.GetAsync($"/Culture/Set?culture={culture}&returnUrl=/");
        Assert.AreEqual(HttpStatusCode.Found, cultureResponse.StatusCode);

        var response = await Http.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("Aiursoft Corporation", html);
        Assert.DoesNotContain("苏州艾软科技有限公司", html);
    }
}
