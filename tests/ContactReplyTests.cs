using System.Data.Common;
using System.Net;
using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Models;
using Aiursoft.AiurProtocol.Services;
using Aiursoft.AlertCenter.SDK.Models;
using Newtonsoft.Json.Linq;
using Aiursoft.CorpHome.Authorization;
using Aiursoft.CorpHome.InMemory;
using Aiursoft.CorpHome.Models.ContactsViewModels;
using Aiursoft.DbTools;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using static Aiursoft.WebTools.Extends;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.BackgroundJobs;
using Aiursoft.CorpHome.Services.ContactReplies;
using Aiursoft.CorpHome.Sqlite;
using Aiursoft.CorpHome.Tests.IntegrationTests;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Aiursoft.CorpHome.Tests;

[TestClass]
public class ContactReplyTests
{
    [TestMethod]
    public async Task QueueUsesStoredRecipientAndTrustedEngineerAddressAndRejectsSecondReply()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await using var db = database.Open();
        var contact = new Contact { Email = "customer@example.invalid" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var service = new ContactReplyService(db, Options.Create(EmailSettings()));

        Assert.IsNull(await service.QueueAsync(contact.Id, "engineer", " Subject ", " Body "));
        Assert.IsNotNull(await service.QueueAsync(contact.Id, "engineer", "Second", "Second"));

        var reply = await db.ContactReplies.SingleAsync();
        Assert.AreEqual(contact.Email, reply.Recipient);
        Assert.AreEqual("trusted@example.invalid", reply.ReplyTo);
        Assert.AreEqual("company@example.invalid", reply.Sender);
        Assert.AreEqual("engineer", reply.AuthorId);
        Assert.AreEqual("Subject", reply.Subject);
        Assert.AreEqual("Body", reply.Body);

        db.ContactReplies.Add(new ContactReply { ContactId = contact.Id });
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("not-an-address")]
    public async Task QueueRejectsMissingOrInvalidTrustedEngineerAddress(string? address)
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await using var db = database.Open();
        var contact = new Contact { Email = "customer@example.invalid" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var settings = EmailSettings();
        settings.EngineerAddresses.Clear();
        if (address != null) settings.EngineerAddresses["engineer"] = address;

        var error = await new ContactReplyService(db, Options.Create(settings))
            .QueueAsync(contact.Id, "engineer", "Subject", "Body");

        Assert.IsNotNull(error);
        Assert.Contains("trusted company reply address", error);
        Assert.AreEqual(0, await db.ContactReplies.CountAsync());
    }

    [TestMethod]
    public async Task SuccessfulSmtpPersistsSentBeforeResolvingAndNeverRepeats()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        var sender = new Sender();
        var resolver = new SentCheckingResolver(database);
        await using var db = database.Open();
        var job = Job(db, sender, resolver);

        await job.ExecuteAsync();
        await job.ExecuteAsync();

        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(1, resolver.Calls);
        var reply = await database.ReadAsync();
        Assert.AreEqual("sent", reply.Status);
        Assert.IsNotNull(reply.AttemptedAtUtc);
        Assert.IsNotNull(reply.IncidentResolvedAtUtc);
        Assert.IsNull(reply.Error);
    }

    [TestMethod]
    public async Task ResolutionFailureRetriesOnlyResolution()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        var sender = new Sender();
        var resolver = new Resolver(_ => throw new HttpRequestException("Local resolution unavailable"));
        await using var db = database.Open();
        var job = Job(db, sender, resolver);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => job.ExecuteAsync());
        var failed = await database.ReadAsync();
        Assert.AreEqual("sent", failed.Status);
        Assert.IsNull(failed.IncidentResolvedAtUtc);
        resolver.Action = _ => Task.CompletedTask;
        await job.ExecuteAsync();
        await job.ExecuteAsync();

        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(2, resolver.Calls);
        Assert.IsNotNull((await database.ReadAsync()).IncidentResolvedAtUtc);
    }

    [TestMethod]
    public async Task SmtpExceptionBecomesUnknownWithoutClosingOrResending()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        var sender = new Sender(_ => throw new HttpRequestException("SMTP acceptance unknown"));
        var resolver = new Resolver();
        await using var db = database.Open();
        var job = Job(db, sender, resolver);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => job.ExecuteAsync());
        await job.ExecuteAsync();

        var reply = await database.ReadAsync();
        Assert.AreEqual("unknown", reply.Status);
        Assert.IsNotNull(reply.AttemptedAtUtc);
        Assert.IsNotNull(reply.Error);
        Assert.IsNull(reply.SentAtUtc);
        Assert.IsNull(reply.IncidentResolvedAtUtc);
        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(0, resolver.Calls);
    }

    [TestMethod]
    public async Task AbandonedSendingIsNeitherResentNorClosed()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync("sending");
        var sender = new Sender();
        var resolver = new Resolver();
        await using var db = database.Open();

        await Job(db, sender, resolver).ExecuteAsync();

        Assert.AreEqual("sending", (await database.ReadAsync()).Status);
        Assert.AreEqual(0, sender.Calls);
        Assert.AreEqual(0, resolver.Calls);
    }

    [TestMethod]
    public async Task FailedLocalSentSaveLeavesSendingAndDoesNotResend()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        var sender = new Sender();
        var resolver = new Resolver();
        var failure = new FailSentSave();
        await using (var db = database.Open(failure))
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(db, sender, resolver).ExecuteAsync());

        var reply = await database.ReadAsync();
        Assert.IsTrue(failure.Failed);
        Assert.AreEqual("sending", reply.Status);
        Assert.IsNotNull(reply.AttemptedAtUtc);
        Assert.IsNull(reply.SentAtUtc);
        Assert.IsNull(reply.IncidentResolvedAtUtc);
        await using (var retry = database.Open())
            await Job(retry, sender, resolver).ExecuteAsync();
        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(0, resolver.Calls);
    }

    [TestMethod]
    public async Task ConcurrentStalePendingClaimSendsOnlyOnceOnSqlite()
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        var gate = new PauseClaim();
        var sender = new Sender();
        var resolver = new Resolver();
        await using var firstDb = database.Open(gate);
        await using var secondDb = database.Open();
        var firstRun = Job(firstDb, sender, resolver).ExecuteAsync();
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Job(secondDb, sender, resolver).ExecuteAsync();
        }
        finally
        {
            gate.Release.TrySetResult();
            await firstRun.WaitAsync(TimeSpan.FromSeconds(10));
        }

        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(1, resolver.Calls);
        Assert.IsNotNull((await database.ReadAsync()).IncidentResolvedAtUtc);
    }

    [TestMethod]
    [DataRow("match")]
    [DataRow("resolved")]
    [DataRow("missing")]
    [DataRow("failure")]
    public async Task SdkResolverMatchesReportAndSourceAndRetriesUnconfirmedClosure(string scenario)
    {
        await using var database = await ReplyDatabase.CreateAsync();
        await database.SeedAsync();
        await using var db = database.Open();
        var contact = await db.Contacts.SingleAsync();
        var matchingId = Guid.NewGuid();
        var wrongReportId = Guid.NewGuid();
        var wrongSourceId = Guid.NewGuid();
        var resolvedPaths = new List<string>();
        var queries = new List<string>();
        var state = new HttpScenario { FailResolution = scenario == "failure", IncludeMatch = scenario != "missing" };
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 18182));
        await using var host = builder.Build();
        host.MapGet("/health", () => Results.Ok());
        host.MapMethods("/{**path}", ["GET", "POST", "PUT", "PATCH"], (HttpContext context) =>
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(context.Request.Headers.Authorization));
            if (context.Request.Method == "GET")
            {
                queries.Add(context.Request.QueryString.Value!);
                var incidents = new List<IncidentDetails>
                {
                    new() { IncidentId = wrongReportId, ReportId = Guid.NewGuid(), Status = "open", Version = Guid.NewGuid(),
                        Metadata = JObject.FromObject(new { source = "CorpHome.ContactSubmission" }) },
                    new() { IncidentId = wrongSourceId, ReportId = contact.AlertCenterReportId!.Value, Status = "open", Version = Guid.NewGuid(),
                        Metadata = JObject.FromObject(new { source = "Other.Source" }) }
                };
                if (state.IncludeMatch) incidents.Add(new IncidentDetails { IncidentId = matchingId, ReportId = contact.AlertCenterReportId!.Value,
                    Status = scenario == "resolved" ? "resolved" : "open", Version = Guid.NewGuid(),
                    Metadata = JObject.FromObject(new { source = "CorpHome.ContactSubmission" }) });
                return Results.Text(JsonConvert.SerializeObject(new QueryIncidentsResponse { Message = "Local query", Code = Code.JobDone, Page = 1, PageSize = 100,
                    TotalCount = incidents.Count, Items = incidents }), "application/json");
            }
            resolvedPaths.Add(context.Request.Path.Value!);
            return Results.Text(JsonConvert.SerializeObject(new ResolveIncidentResponse { Message = "Local resolution", Code = state.FailResolution ? (Code)(-1) : Code.JobDone }),
                "application/json");
        });
        await host.StartAsync();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient();
            services.AddAiurProtocolClient();
            await using var provider = services.BuildServiceProvider();
            using var healthClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
            var endpoint = host.Urls.Single();
            using var health = await healthClient.GetAsync($"{endpoint}/health");
            Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SettingsMap.AlertCenterEnabled] = "True", [SettingsMap.AlertCenterEndpoint] = endpoint,
                [SettingsMap.AlertCenterApiKey] = "local-test-key",
                [SettingsMap.AlertCenterPublicBaseUrl] = "https://company.example.invalid"
            }).Build();
            var settings = new GlobalSettingsService(db, configuration, null!, cache,
                new EphemeralDataProtectionProvider(), NullLogger<GlobalSettingsService>.Instance);
            var resolver = new ContactIncidentResolver(provider.GetRequiredService<AiurProtocolClient>(),
                new AlertCenterSettingsProvider(settings));
            var sender = new Sender();
            var job = Job(db, sender, resolver);

            if (scenario is "missing" or "failure")
            {
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => job.ExecuteAsync());
                var failed = await database.ReadAsync();
                Assert.AreEqual("sent", failed.Status);
                Assert.IsNull(failed.IncidentResolvedAtUtc);
                if (scenario == "missing") Assert.AreEqual(0, resolvedPaths.Count);
                state.IncludeMatch = true;
                state.FailResolution = false;
            }
            await job.ExecuteAsync();
            await job.ExecuteAsync();

            Assert.AreEqual(1, sender.Calls);
            Assert.IsNotNull((await database.ReadAsync()).IncidentResolvedAtUtc);
            Assert.AreEqual(scenario == "resolved" ? 0 : scenario == "failure" ? 2 : 1, resolvedPaths.Count);
            Assert.IsTrue(resolvedPaths.All(path => path.Contains(matchingId.ToString(), StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(queries.Count > 0);
            Assert.IsTrue(queries.All(query => query.Contains("contactId", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(queries.All(query => query.Contains($"MetadataValue={contact.Id}", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [TestMethod]
    public async Task RealSmtpSenderUsesFixedEnvelopeAndPlainTextHeaders()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 18183);
        listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var receipt = ReceiveMailAsync(listener, timeout.Token);
        try
        {
            var settings = EmailSettings();
            settings.Host = "127.0.0.1";
            settings.Port = 18183;
            settings.RequireTls = false;
            await new ContactEmailSender(Options.Create(settings)).SendAsync(new ContactReply
            {
                Sender = settings.From, Recipient = "customer@example.invalid", ReplyTo = "trusted@example.invalid",
                Subject = "A reply", Body = "Plain text answer"
            });
            var message = await receipt;
            Assert.Contains("MAIL FROM:<company@example.invalid>", message);
            Assert.Contains("RCPT TO:<customer@example.invalid>", message);
            Assert.Contains("From: company@example.invalid", message);
            Assert.Contains("To: customer@example.invalid", message);
            Assert.Contains("Reply-To: trusted@example.invalid", message);
            Assert.Contains("Content-Type: text/plain", message);
            Assert.DoesNotContain("text/html", message);
            var encodedBody = message[(message.IndexOf("\n\n", StringComparison.Ordinal) + 2)..].Trim();
            Assert.AreEqual("Plain text answer", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedBody)));
        }
        finally
        {
            await timeout.CancelAsync();
            listener.Stop();
            try { await receipt; }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
        }
    }

    private static async Task<string> ReceiveMailAsync(System.Net.Sockets.TcpListener listener, CancellationToken token)
    {
        using var client = await listener.AcceptTcpClientAsync(token);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.AutoFlush = true;
        writer.NewLine = "\r\n";
        var received = new System.Text.StringBuilder();
        await writer.WriteLineAsync("220 localhost test SMTP");
        while (await reader.ReadLineAsync(token) is { } line)
        {
            received.AppendLine(line);
            if (line.StartsWith("EHLO", StringComparison.Ordinal))
                await writer.WriteLineAsync("250 localhost");
            else if (line == "DATA")
            {
                await writer.WriteLineAsync("354 End with dot");
                while (await reader.ReadLineAsync(token) is { } bodyLine && bodyLine != ".")
                    received.AppendLine(bodyLine);
                await writer.WriteLineAsync("250 Accepted");
                return received.ToString();
            }
            else await writer.WriteLineAsync("250 OK");
        }
        throw new IOException("SMTP client disconnected before DATA acceptance.");
    }

    [TestClass]
    public class ContactReplyHttpTests : TestBase
    {
        [TestInitialize]
        public override async Task CreateServer()
        {
            Server = await AppAsync<ReplyStartup>([], port: 18181);
            await Server.UpdateDbAsync<TemplateDbContext>();
            await Server.SeedAsync();
            await Server.StartAsync();
            Http.BaseAddress = new Uri("http://localhost:18181");
            using var health = await Http.GetAsync("/Home/Index");
            health.EnsureSuccessStatusCode();
        }

        [TestCleanup]
        public override async Task CleanServer()
        {
            await base.CleanServer();
            Http.Dispose();
        }

        [TestMethod]
        public async Task AdminReplyRequiresCsrfAndIgnoresForgedContactAndRecipient()
        {
            await LoginAsAdmin();
            using var scope = Server!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var contact = new Contact { Email = "stored@example.invalid" };
            var other = new Contact { Email = "other@example.invalid" };
            db.Contacts.AddRange(contact, other);
            await db.SaveChangesAsync();
            var admin = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync("admin@default.com");
            var email = scope.ServiceProvider.GetRequiredService<IOptions<ContactEmailSettings>>().Value;
            email.EngineerAddresses[admin!.Id] = "trusted@example.invalid";
            var path = $"/Contacts/Reply/{contact.Id}";
            using var page = await Http.GetAsync(path);
            page.EnsureSuccessStatusCode();
            var form = new Dictionary<string, string>
            {
                ["Subject"] = "Reply", ["Body"] = "Answer", ["Contact.Id"] = other.Id.ToString(),
                ["Contact.Email"] = "attacker@example.invalid", ["Recipient"] = "attacker@example.invalid",
                ["Replies[0].Recipient"] = "attacker@example.invalid"
            };
            using var rejected = await PostForm(path, form, includeToken: false);
            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.AreEqual(0, await db.ContactReplies.CountAsync());
            using var accepted = await PostForm(path, form);
            AssertRedirect(accepted, path);
            var reply = await db.ContactReplies.SingleAsync();
            Assert.AreEqual(contact.Id, reply.ContactId);
            Assert.AreEqual(contact.Email, reply.Recipient);
            Assert.AreEqual("trusted@example.invalid", reply.ReplyTo);
            Assert.AreEqual("company@example.invalid", reply.Sender);
        }

        [TestMethod]
        public async Task ViewOnlyUserCannotGetOrPostReply()
        {
            using var scope = Server!.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = new User { DisplayName = "Viewer", UserName = "viewer@example.invalid", Email = "viewer@example.invalid", EmailConfirmed = true };
            Assert.IsTrue((await users.CreateAsync(user, "Viewer@123456!")).Succeeded);
            Assert.IsTrue((await users.AddClaimAsync(user, new Claim(AppPermissions.Type, AppPermissionNames.CanViewContacts))).Succeeded);
            using var login = await PostForm("/Account/Login", new Dictionary<string, string>
            {
                ["EmailOrUserName"] = user.Email, ["Password"] = "Viewer@123456!"
            });
            using var index = await Http.GetAsync("/Contacts/Index");
            index.EnsureSuccessStatusCode();
            using var get = await Http.GetAsync("/Contacts/Reply/1");
            AssertDenied(get);
            using var post = await PostForm("/Contacts/Reply/1", new Dictionary<string, string>
            {
                ["Subject"] = "Reply", ["Body"] = "Answer"
            }, tokenUrl: "/Manage/ChangePassword");
            AssertDenied(post);
        }

        private static void AssertDenied(HttpResponseMessage response)
        {
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            Assert.Contains("/Error/Code403", response.Headers.Location!.OriginalString);
        }

        public sealed class ReplyStartup : Startup, IWebStartup
        {
            public new void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
            {
                base.ConfigureServices(configuration, environment, services);
                var name = Guid.NewGuid().ToString();
                services.RemoveAll<DbContextOptions<InMemoryContext>>();
                services.AddDbContext<InMemoryContext>(options => options.UseInMemoryDatabase(name));
                services.PostConfigure<ContactEmailSettings>(settings =>
                {
                    settings.Host = "smtp.example.invalid";
                    settings.From = "company@example.invalid";
                });
            }
        }
    }

    [TestMethod]
    public void DisplayOnlyReplyPropertiesAreNeitherBoundNorValidated()
    {
        foreach (var name in new[] { nameof(ReplyViewModel.Contact), nameof(ReplyViewModel.Replies) })
        {
            var property = typeof(ReplyViewModel).GetProperty(name)!;
            Assert.IsTrue(Attribute.IsDefined(property, typeof(BindNeverAttribute)));
            Assert.IsTrue(Attribute.IsDefined(property, typeof(ValidateNeverAttribute)));
        }
    }

    private sealed class HttpScenario
    {
        public bool IncludeMatch { get; set; }
        public bool FailResolution { get; set; }
    }

    private static ContactEmailSettings EmailSettings() => new()
    {
        Host = "smtp.example.invalid", From = "company@example.invalid",
        EngineerAddresses = new() { ["engineer"] = "trusted@example.invalid" }
    };

    private static ContactReplyDeliveryJob Job(TemplateDbContext db, IContactEmailSender sender,
        IContactIncidentResolver resolver) => new(db, sender, resolver, NullLogger<ContactReplyDeliveryJob>.Instance);

    private sealed class Sender(Func<ContactReply, Task>? action = null) : IContactEmailSender
    {
        public int Calls { get; private set; }
        public Task SendAsync(ContactReply reply)
        {
            Calls++;
            return action?.Invoke(reply) ?? Task.CompletedTask;
        }
    }

    private sealed class SentCheckingResolver(ReplyDatabase database) : IContactIncidentResolver
    {
        public int Calls { get; private set; }
        public async Task ResolveAsync(Contact contact)
        {
            Calls++;
            var persisted = await database.ReadAsync();
            Assert.AreEqual("sent", persisted.Status);
            Assert.IsNotNull(persisted.SentAtUtc);
        }
    }

    private sealed class Resolver(Func<Contact, Task>? action = null) : IContactIncidentResolver
    {
        public int Calls { get; private set; }
        public Func<Contact, Task> Action { get; set; } = action ?? (_ => Task.CompletedTask);
        public Task ResolveAsync(Contact contact)
        {
            Calls++;
            return Action(contact);
        }
    }

    private sealed class FailSentSave : SaveChangesInterceptor
    {
        public bool Failed { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ContactReply>().Any(entry => entry.Entity.Status == "sent"))
            {
                Failed = true;
                throw new DbUpdateException("Simulated sent acknowledgement persistence failure");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class PauseClaim : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }

    private sealed class ReplyDatabase(SqliteConnection anchor) : IAsyncDisposable
    {
        public static async Task<ReplyDatabase> CreateAsync()
        {
            var anchor = new SqliteConnection($"Data Source=contact-replies-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            await anchor.OpenAsync();
            var database = new ReplyDatabase(anchor);
            await using var db = database.Open();
            await db.Database.EnsureCreatedAsync();
            return database;
        }

        public SqliteContext Open(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<SqliteContext>()
            .UseSqlite(anchor.ConnectionString).AddInterceptors(interceptors).Options);

        public async Task SeedAsync(string status = "pending")
        {
            await using var db = Open();
            db.ContactReplies.Add(new ContactReply
            {
                Contact = new Contact { Email = "customer@example.invalid", AlertCenterReportId = Guid.NewGuid(),
                    AlertCenterReportedAtUtc = DateTime.UtcNow },
                Recipient = "customer@example.invalid", Sender = "company@example.invalid",
                ReplyTo = "trusted@example.invalid", Subject = "Subject", Body = "Body", Status = status
            });
            await db.SaveChangesAsync();
        }

        public async Task<ContactReply> ReadAsync()
        {
            await using var db = Open();
            return await db.ContactReplies.AsNoTracking().SingleAsync();
        }

        public ValueTask DisposeAsync() => anchor.DisposeAsync();
    }
}
