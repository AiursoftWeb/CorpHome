using System.Net;
using Aiursoft.AiurProtocol.Models;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.InMemory;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.BackgroundJobs;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Aiursoft.CorpHome.Sqlite;
using Aiursoft.DbTools;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.CorpHome.Tests.IntegrationTests;

[TestClass]
public class ContactIncidentSyncTests : TestBase
{
    private const string PublicBaseUrl = "https://corphome.example.invalid/company";
    private const string TestApiKey = "local-test-key-not-a-real-credential";

    [TestInitialize]
    public override async Task CreateServer()
    {
        using var reservation = new TestPortReservation();
        Http.BaseAddress = new Uri($"http://localhost:{reservation.Port}");
        Server = await AppAsync<ContactIncidentTestStartup>([], port: reservation.Port);
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
    public async Task DefaultDisabledRetainsPendingAndEnablingDeliversOnlyOnce()
    {
        var contact = PendingContact();
        await AddContactsAsync(contact);
        var client = new RecordingClient(Success);

        await RunJobAsync(client);
        Assert.AreEqual(0, client.Requests.Count);
        Assert.IsNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);

        await ConfigureAsync();
        await RunJobAsync(client);
        await RunJobAsync(client);

        Assert.AreEqual(1, client.Requests.Count);
        Assert.AreEqual(contact.AlertCenterReportId, client.Requests.Single().ReportId);
        Assert.IsNotNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
    }

    [TestMethod]
    public async Task FailedDeliveryDoesNotBlockOtherPagesOrSendLegacyAndReportedContacts()
    {
        await ConfigureAsync();
        var legacy = new Contact { Email = "legacy@example.invalid" };
        var reported = PendingContact();
        reported.AlertCenterReportedAtUtc = DateTime.UtcNow.AddDays(-1);
        var pending = Enumerable.Range(0, 103).Select(_ => PendingContact()).ToArray();
        await AddContactsAsync([legacy, reported, .. pending]);
        var client = new RecordingClient(request => request.ReportId == pending[0].AlertCenterReportId
            ? throw new HttpRequestException("Simulated disconnection")
            : Success(request));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => RunJobAsync(client));

        CollectionAssert.AreEqual(pending.Select(contact => contact.AlertCenterReportId!.Value).ToArray(),
            client.Requests.Select(request => request.ReportId).ToArray());
        Assert.IsNull((await ReadContactAsync(pending[0].Id)).AlertCenterReportedAtUtc);
        Assert.IsNotNull((await ReadContactAsync(pending[^1].Id)).AlertCenterReportedAtUtc);
        Assert.AreEqual(reported.AlertCenterReportedAtUtc, (await ReadContactAsync(reported.Id)).AlertCenterReportedAtUtc);
        Assert.IsNull((await ReadContactAsync(legacy.Id)).AlertCenterReportId);

        var request = client.Requests[^1];
        Assert.AreEqual("sev2", request.Level);
        Assert.AreEqual(new DateTimeOffset(pending[^1].CreateTime), request.OccurredAtUtc);
        Assert.IsNotNull(request.Content);
        Assert.Contains($"{PublicBaseUrl}/Contacts/Reply/{pending[^1].Id}", request.Content);
        var payload = JsonConvert.SerializeObject(request);
        foreach (var privateValue in new[] { pending[^1].Email, pending[^1].FirstName, pending[^1].LastName,
                     pending[^1].Company, pending[^1].JobTitle, pending[^1].ServicesProvided })
            Assert.DoesNotContain(privateValue, payload);
    }

    [TestMethod]
    public async Task LostResponseRetainsPendingAndRetriesTheSameReportIdUntilReceiptIsSaved()
    {
        await ConfigureAsync();
        var contact = PendingContact();
        await AddContactsAsync(contact);
        var remoteIncidents = new Dictionary<Guid, Guid>();
        var client = new RecordingClient(request =>
        {
            if (remoteIncidents.TryAdd(request.ReportId, Guid.NewGuid()))
                throw new HttpRequestException("Remote commit succeeded but response was lost");
            return new CreateIncidentResponse
            {
                Code = Code.JobDone,
                ReportId = request.ReportId,
                IncidentId = remoteIncidents[request.ReportId],
                Duplicate = true
            };
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => RunJobAsync(client));
        Assert.IsNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
        await RunJobAsync(client);
        await RunJobAsync(client);

        Assert.AreEqual(1, remoteIncidents.Count);
        Assert.AreEqual(2, client.Requests.Count);
        Assert.IsTrue(client.Requests.All(request => request.ReportId == contact.AlertCenterReportId));
        Assert.IsNotNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
    }

    [TestMethod]
    [DataRow("report-id")]
    [DataRow("incident-id")]
    [DataRow("code")]
    public async Task InvalidReceiptNeverMarksReportedAndCanRetry(string invalidField)
    {
        await ConfigureAsync();
        var contact = PendingContact();
        await AddContactsAsync(contact);
        var client = new RecordingClient(request =>
        {
            var response = Success(request);
            switch (invalidField)
            {
                case "report-id": response.ReportId = Guid.NewGuid(); break;
                case "incident-id": response.IncidentId = Guid.Empty; break;
                case "code": response.Code = (Code)(-1); break;
            }
            return response;
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => RunJobAsync(client));
        Assert.IsNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
        client.Respond = Success;
        await RunJobAsync(client);
        Assert.AreEqual(contact.AlertCenterReportId, client.Requests[1].ReportId);
        Assert.IsNotNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
    }

    [TestMethod]
    public async Task LocalSaveFailureStaysPendingWhileLaterDeliverySavesAndRetriesOriginalReportId()
    {
        await ConfigureAsync();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var failure = new FailOneSaveInterceptor();
        var options = new DbContextOptionsBuilder<SqliteContext>().UseSqlite(connection).AddInterceptors(failure).Options;
        await using var db = new SqliteContext(options);
        await db.Database.EnsureCreatedAsync();
        var first = PendingContact();
        var second = PendingContact();
        db.Contacts.AddRange(first, second);
        await db.SaveChangesAsync();
        failure.FailNextSave = true;
        var client = new RecordingClient(Success);
        using var scope = Server!.Services.CreateScope();
        var job = new ContactIncidentSyncJob(db,
            scope.ServiceProvider.GetRequiredService<AlertCenterSettingsProvider>(), client,
            NullLogger<ContactIncidentSyncJob>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => job.ExecuteAsync());

        Assert.IsFalse(failure.FailNextSave);
        Assert.IsNull((await db.Contacts.AsNoTracking().SingleAsync(contact => contact.Id == first.Id)).AlertCenterReportedAtUtc,
            "Saving the second contact must not accidentally persist the first contact's failed acknowledgement.");
        Assert.IsNotNull((await db.Contacts.AsNoTracking().SingleAsync(contact => contact.Id == second.Id)).AlertCenterReportedAtUtc);
        await job.ExecuteAsync();
        await job.ExecuteAsync();
        CollectionAssert.AreEqual(new[] { first.AlertCenterReportId!.Value, second.AlertCenterReportId!.Value, first.AlertCenterReportId.Value },
            client.Requests.Select(request => request.ReportId).ToArray());
        Assert.IsNotNull((await db.Contacts.AsNoTracking().SingleAsync(contact => contact.Id == first.Id)).AlertCenterReportedAtUtc);
    }

    [TestMethod]
    public async Task RegisteredSdkClientDeliversToLocalHttpServerWithAuthenticationAndReceipt()
    {
        using var reservation = new TestPortReservation();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{reservation.Port}");
        await using var fakeServer = builder.Build();
        var requests = new List<CreateIncidentRequest>();
        var authorizationHeaders = new List<string>();
        fakeServer.MapGet("/health", () => Results.Ok());
        fakeServer.MapPost("/api/v1/incidents", async (HttpContext context) =>
        {
            using var body = new StreamReader(context.Request.Body);
            var request = JsonConvert.DeserializeObject<CreateIncidentRequest>(await body.ReadToEndAsync())!;
            requests.Add(request);
            authorizationHeaders.Add(context.Request.Headers.Authorization.ToString());
            return Results.Text(JsonConvert.SerializeObject(Success(request)), "application/json");
        });
        reservation.ConfigureServer(fakeServer);
        await fakeServer.StartAsync();
        try
        {
            using var http = Server!.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
            var endpoint = $"http://localhost:{reservation.Port}";
            using var health = await http.GetAsync($"{endpoint}/health");
            Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
            await ConfigureAsync(endpoint);
            var contact = PendingContact();
            await AddContactsAsync(contact);

            using (var scope = Server!.Services.CreateScope())
            {
                Assert.IsInstanceOfType<ContactIncidentClient>(scope.ServiceProvider.GetRequiredService<IContactIncidentClient>());
                await scope.ServiceProvider.GetRequiredService<ContactIncidentSyncJob>().ExecuteAsync();
            }
            using (var scope = Server.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<ContactIncidentSyncJob>().ExecuteAsync();

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(contact.AlertCenterReportId, requests[0].ReportId);
            Assert.AreEqual($"Bearer {TestApiKey}", authorizationHeaders.Single());
            Assert.IsNotNull((await ReadContactAsync(contact.Id)).AlertCenterReportedAtUtc);
        }
        finally
        {
            await fakeServer.StopAsync();
        }
    }

    [TestMethod]
    public async Task SqliteUpgradeLeavesHistoricalContactUnqueued()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteContext>().UseSqlite(connection).Options;
        await using var db = new SqliteContext(options);
        await db.GetService<IMigrator>().MigrateAsync("20260301071502_IncreaseAvatarPathLength");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Contacts (OrganizationSize, ConsumeOpenSource, ServicesProvided, FirstName, LastName,
                Company, JobTitle, Email, AgreeToInformation, AgreeToPrivacy, CreateTime)
            VALUES ('Small', 'Use', 'Development', 'Historical', 'Contact', 'Example', 'Engineer',
                'historical@example.invalid', 1, 1, '2026-01-01 00:00:00');
            """);

        await db.Database.MigrateAsync();

        var historical = await db.Contacts.AsNoTracking().SingleAsync();
        Assert.AreEqual("historical@example.invalid", historical.Email);
        Assert.IsNull(historical.AlertCenterReportId);
        Assert.IsNull(historical.AlertCenterReportedAtUtc);
        Assert.IsFalse(db.Database.HasPendingModelChanges());
    }

    private async Task ConfigureAsync(string endpoint = "http://localhost:1")
    {
        using var scope = Server!.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterEnabled, "True");
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterEndpoint, endpoint);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterApiKey, TestApiKey);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterPublicBaseUrl, PublicBaseUrl);
    }

    private async Task AddContactsAsync(params Contact[] contacts)
    {
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        db.Contacts.AddRange(contacts);
        await db.SaveChangesAsync();
    }

    private async Task<Contact> ReadContactAsync(int id)
    {
        using var scope = Server!.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<TemplateDbContext>().Contacts.AsNoTracking()
            .SingleAsync(contact => contact.Id == id);
    }

    private async Task RunJobAsync(IContactIncidentClient client)
    {
        using var scope = Server!.Services.CreateScope();
        var job = new ContactIncidentSyncJob(scope.ServiceProvider.GetRequiredService<TemplateDbContext>(),
            scope.ServiceProvider.GetRequiredService<AlertCenterSettingsProvider>(), client,
            NullLogger<ContactIncidentSyncJob>.Instance);
        await job.ExecuteAsync();
    }

    private static Contact PendingContact() => new()
    {
        AlertCenterReportId = Guid.NewGuid(),
        FirstName = "PrivateFirstName",
        LastName = "PrivateLastName",
        Company = "PrivateCompany",
        JobTitle = "PrivateJobTitle",
        ServicesProvided = "PrivateInquiry",
        Email = "private-contact@example.invalid",
        AgreeToPrivacy = true,
        CreateTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    };

    private static CreateIncidentResponse Success(CreateIncidentRequest request) => new()
    {
        Code = Code.JobDone,
        Message = "Incident created by local test server",
        ReportId = request.ReportId,
        IncidentId = Guid.NewGuid(),
        ReceivedAtUtc = DateTimeOffset.UtcNow
    };

    public sealed class ContactIncidentTestStartup : Startup, IWebStartup
    {
        public new void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
        {
            base.ConfigureServices(configuration, environment, services);
            var databaseName = Guid.NewGuid().ToString();
            services.RemoveAll<DbContextOptions<InMemoryContext>>();
            services.AddDbContext<InMemoryContext>(options => options.UseInMemoryDatabase(databaseName));
        }
    }

    private sealed class RecordingClient(Func<CreateIncidentRequest, CreateIncidentResponse> respond) : IContactIncidentClient
    {
        public List<CreateIncidentRequest> Requests { get; } = [];
        public Func<CreateIncidentRequest, CreateIncidentResponse> Respond { get; set; } = respond;

        public Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(Respond(request));
        }
    }

    private sealed class FailOneSaveInterceptor : SaveChangesInterceptor
    {
        public bool FailNextSave { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!FailNextSave) return base.SavingChangesAsync(eventData, result, cancellationToken);
            FailNextSave = false;
            throw new DbUpdateException("Simulated local acknowledgement persistence failure");
        }
    }
}
