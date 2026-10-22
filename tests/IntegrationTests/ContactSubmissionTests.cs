using System.Net;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.InMemory;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Aiursoft.DbTools;
using Aiursoft.WebTools.Abstractions.Models;
using Edi.Captcha;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class ContactSubmissionTests : TestBase
{
    [TestInitialize]
    public override async Task CreateServer()
    {
        using var reservation = new TestPortReservation();
        Http.BaseAddress = new Uri($"http://localhost:{reservation.Port}");
        Server = await AppAsync<ContactSubmissionStartup>([], port: reservation.Port);
        try
        {
            reservation.ConfigureServer(Server);
            await Server.UpdateDbAsync<TemplateDbContext>();
            await Server.SeedAsync();
            await Server.StartAsync();
        }
        catch
        {
            Server.Dispose();
            Server = null;
            throw;
        }
    }

    [TestCleanup]
    public override async Task CleanServer()
    {
        try
        {
            await base.CleanServer();
        }
        finally
        {
            Http.Dispose();
        }
    }

    [TestMethod]
    public async Task ValidSubmissionPersistsPendingReportWithoutCallingAlertCenter()
    {
        using var scope = Server!.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<GlobalSettingsService>()
            .UpdateSettingAsync(SettingsMap.AlertCenterEnabled, "True");
        using var response = await PostForm("/Home/Contact", ValidForm());
        AssertRedirect(response, "/Home/ContactSuccess");

        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        var contact = await db.Contacts.AsNoTracking().SingleAsync();
        Assert.AreEqual("form-test@example.invalid", contact.Email);
        Assert.IsNotNull(contact.AlertCenterReportId);
        Assert.AreNotEqual(Guid.Empty, contact.AlertCenterReportId.Value);
        Assert.IsNull(contact.AlertCenterReportedAtUtc);
        Assert.AreEqual(0, GetService<UnexpectedIncidentClient>().Calls);
    }

    [TestMethod]
    [DataRow("captcha")]
    [DataRow("privacy")]
    [DataRow("model-state")]
    public async Task InvalidSubmissionDoesNotPersistOrQueueContact(string invalidField)
    {
        var form = ValidForm();
        switch (invalidField)
        {
            case "captcha": form["CaptchaCode"] = "0000"; break;
            case "privacy": form["AgreeToPrivacy"] = "false"; break;
            case "model-state": form["Email"] = "not-an-email"; break;
        }

        using var response = await PostForm("/Home/Contact", form);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var scope = Server!.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<TemplateDbContext>().Contacts.CountAsync());
        Assert.AreEqual(0, GetService<UnexpectedIncidentClient>().Calls);
    }

    private static Dictionary<string, string> ValidForm() => new()
    {
        ["OrganizationSize"] = "10-50",
        ["ConsumeOpenSource"] = "Use open source",
        ["ServicesProvided"] = "Software",
        ["FirstName"] = "Form",
        ["LastName"] = "Test",
        ["Company"] = "Example",
        ["JobTitle"] = "Engineer",
        ["Email"] = "form-test@example.invalid",
        ["AgreeToInformation"] = "true",
        ["AgreeToPrivacy"] = "true",
        ["CaptchaCode"] = "1234",
        ["CaptchaToken"] = "test-captcha-token"
    };

    public sealed class ContactSubmissionStartup : Startup, IWebStartup
    {
        public new void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
        {
            base.ConfigureServices(configuration, environment, services);
            var databaseName = Guid.NewGuid().ToString();
            services.RemoveAll<DbContextOptions<InMemoryContext>>();
            services.AddDbContext<InMemoryContext>(options => options.UseInMemoryDatabase(databaseName));
            services.Replace(ServiceDescriptor.Singleton<IStatelessCaptcha, TestCaptcha>());
            services.AddSingleton<UnexpectedIncidentClient>();
            services.Replace(ServiceDescriptor.Singleton<IContactIncidentClient>(provider =>
                provider.GetRequiredService<UnexpectedIncidentClient>()));
        }
    }

    private sealed class TestCaptcha : IStatelessCaptcha
    {
        public StatelessCaptchaResult GenerateCaptcha(int width, int height) => new()
        {
            Token = "test-captcha-token",
            ImageBytes = []
        };

        public bool Validate(string code, string token, bool ignoreCase) =>
            code == "1234" && token == "test-captcha-token";
    }

    private sealed class UnexpectedIncidentClient : IContactIncidentClient
    {
        public int Calls { get; private set; }

        public Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request)
        {
            Calls++;
            throw new InvalidOperationException("A form submission must not depend on AlertCenter availability.");
        }
    }
}
