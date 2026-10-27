using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services;
using Aiursoft.CorpHome.Services.BackgroundJobs;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Aiursoft.CorpHome.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiursoft.CorpHome.Tests;

[TestClass]
public class ContactFollowUpTests
{
    private readonly DateTime _now = DateTime.UtcNow.AddMinutes(-5);
    private SqliteConnection _connection = null!;
    private SqliteContext _db = null!;
    private MemoryCache _cache = null!;
    private IConfigurationRoot _configuration = null!;
    private readonly SaveFailure _failure = new();

    [TestInitialize]
    public async Task Initialize()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new SqliteContext(new DbContextOptionsBuilder<SqliteContext>().UseSqlite(_connection)
            .AddInterceptors(_failure).Options);
        await _db.Database.EnsureCreatedAsync();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"GlobalSettings:{SettingsMap.AlertCenterEnabled}"] = "True",
            [$"GlobalSettings:{SettingsMap.ContactMailEnabled}"] = "True",
            [$"GlobalSettings:{SettingsMap.ContactMailHost}"] = "mail.example.invalid",
            [$"GlobalSettings:{SettingsMap.ContactMailUsername}"] = "followup@example.invalid",
            [$"GlobalSettings:{SettingsMap.ContactMailPassword}"] = "test-only-password",
            [$"GlobalSettings:{SettingsMap.ContactMailStaffSenders}"] = "@example.invalid"
        }).Build();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
        _cache.Dispose();
    }

    [TestMethod]
    public async Task FollowUpMitigatesAllRecentEarlierContactsButNotLaterOldOrOtherCustomers()
    {
        var first = Contact(_now.AddHours(-2));
        var second = Contact(_now.AddHours(-1));
        second.Email = "CUSTOMER@outside.invalid";
        var later = Contact(_now.AddMinutes(1));
        var old = Contact(_now.AddDays(-8));
        var other = Contact(_now.AddHours(-1));
        other.Email = "other@outside.invalid";
        var legacy = Contact(_now.AddHours(-1));
        legacy.AlertCenterReportId = null;
        _db.Contacts.AddRange(first, second, later, old, other, legacy);
        await _db.SaveChangesAsync();
        var mailbox = new Mailbox(new ContactMailboxPage(1, 5, [Email()]));
        var client = new Incidents();

        await Job(mailbox, client).ExecuteAsync();
        await Job(mailbox, client).ExecuteAsync();

        CollectionAssert.AreEquivalent(new[] { first.AlertCenterIncidentId!.Value, second.AlertCenterIncidentId!.Value }, client.Mitigated.ToArray());
        var contacts = await _db.Contacts.AsNoTracking().OrderBy(contact => contact.Id).ToListAsync();
        Assert.IsTrue(contacts.Take(2).All(contact => contact.AlertCenterMitigatedAtUtc != null && contact.FollowUpMessageId!.EndsWith(":1:5")));
        Assert.IsTrue(contacts.Skip(2).All(contact => contact.FollowUpReceivedAtUtc == null));
        Assert.AreEqual(5L, (await _db.ContactMailboxCheckpoints.SingleAsync()).LastUid);
        Assert.AreEqual(5L, mailbox.LastCheckpoint);
    }

    [TestMethod]
    [DataRow("external")]
    [DataRow("lookalike-domain")]
    [DataRow("multiple-from")]
    [DataRow("external-sender")]
    [DataRow("automatic")]
    [DataRow("old-date")]
    [DataRow("future-date")]
    [DataRow("received-before-contact")]
    public async Task UntrustedAutomaticOrOutOfOrderMessagesDoNotMitigate(string kind)
    {
        var contact = Contact(_now.AddHours(-1));
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        var email = Email();
        email = kind switch
        {
            "external" => email with { From = ["attacker@outside.invalid"] },
            "lookalike-domain" => email with { From = ["attacker@fakeexample.invalid"] },
            "multiple-from" => email with { From = ["staff@example.invalid", "other@example.invalid"] },
            "external-sender" => email with { Senders = ["attacker@outside.invalid"] },
            "automatic" => email with { IsAutomatic = true },
            "old-date" => email with { SentAtUtc = _now.AddDays(-2) },
            "future-date" => email with { SentAtUtc = DateTime.UtcNow.AddHours(1) },
            "received-before-contact" => email with { ReceivedAtUtc = _now.AddHours(-2) },
            _ => throw new InvalidOperationException()
        };
        var client = new Incidents();
        await Job(new Mailbox(new ContactMailboxPage(1, 5, [email])), client).ExecuteAsync();
        Assert.AreEqual(0, client.Mitigated.Count);
        Assert.IsNull((await _db.Contacts.AsNoTracking().SingleAsync()).FollowUpReceivedAtUtc);
    }

    [TestMethod]
    public async Task EvidenceBeforeIncidentCreationSurvivesRestartAndMitigationFailure()
    {
        var contact = Contact(_now.AddHours(-1));
        contact.AlertCenterIncidentId = null;
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        var mailbox = new Mailbox(new ContactMailboxPage(1, 5, [Email()]));
        var client = new Incidents { Fail = true };
        await Job(mailbox, client).ExecuteAsync();
        Assert.IsNotNull((await _db.Contacts.AsNoTracking().SingleAsync()).FollowUpMessageId);

        _db.ChangeTracker.Clear();
        var saved = await _db.Contacts.SingleAsync();
        saved.AlertCenterIncidentId = Guid.NewGuid();
        await _db.SaveChangesAsync();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(mailbox, client).ExecuteAsync());
        Assert.IsNull((await _db.Contacts.AsNoTracking().SingleAsync()).AlertCenterMitigatedAtUtc);
        client.Fail = false;
        mailbox.Fail = true;
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(mailbox, client).ExecuteAsync());
        Assert.IsNotNull((await _db.Contacts.AsNoTracking().SingleAsync()).AlertCenterMitigatedAtUtc,
            "A mailbox outage must not block previously saved follow-up evidence.");
        mailbox.Fail = false;
        await Job(mailbox, client).ExecuteAsync();
        Assert.AreEqual(1, client.Mitigated.Count);
    }

    [TestMethod]
    public async Task FailedEvidenceSaveDoesNotAdvanceCheckpointAndMailIsRetried()
    {
        _db.Contacts.Add(Contact(_now.AddHours(-1)));
        await _db.SaveChangesAsync();
        _failure.FailNext = true;
        var mailbox = new Mailbox(new ContactMailboxPage(1, 5, [Email()]));
        var client = new Incidents();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(mailbox, client).ExecuteAsync());
        Assert.AreEqual(0, await _db.ContactMailboxCheckpoints.CountAsync());
        Assert.IsNull((await _db.Contacts.AsNoTracking().SingleAsync()).FollowUpReceivedAtUtc);
        await Job(mailbox, client).ExecuteAsync();
        Assert.AreEqual(1, client.Mitigated.Count);
        Assert.AreEqual(5L, (await _db.ContactMailboxCheckpoints.SingleAsync()).LastUid);
    }

    [TestMethod]
    public async Task FailedMitigationSaveRetriesWithoutLosingEvidence()
    {
        var contact = Contact(_now.AddHours(-1));
        contact.FollowUpReceivedAtUtc = _now;
        contact.FollowUpMessageId = "saved-evidence";
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        _failure.FailNext = true;
        var client = new Incidents();
        var mailbox = new Mailbox();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(mailbox, client).ExecuteAsync());
        Assert.IsNull((await _db.Contacts.AsNoTracking().SingleAsync()).AlertCenterMitigatedAtUtc);
        await Job(mailbox, client).ExecuteAsync();
        Assert.IsNotNull((await _db.Contacts.AsNoTracking().SingleAsync()).AlertCenterMitigatedAtUtc);
        Assert.AreEqual(2, client.Mitigated.Count);
    }

    [TestMethod]
    [DataRow(SettingsMap.ContactMailEnabled)]
    [DataRow(SettingsMap.AlertCenterEnabled)]
    public async Task DisabledIntegrationDoesNotReadMail(string disabledSetting)
    {
        _configuration[$"GlobalSettings:{disabledSetting}"] = "False";
        var mailbox = new Mailbox { Fail = true };
        await Job(mailbox, new Incidents()).ExecuteAsync();
        Assert.AreEqual(0, mailbox.Calls);
    }

    [TestMethod]
    public void SenderAllowlistUsesExactAddressesOrDomainBoundaries()
    {
        var settings = new ContactMailSettings { StaffSenders = ["staff@example.invalid", "@another.invalid"] };
        Assert.IsTrue(settings.AllowsSender("STAFF@example.invalid"));
        Assert.IsTrue(settings.AllowsSender("person@another.invalid"));
        Assert.IsFalse(settings.AllowsSender("other@example.invalid"));
        Assert.IsFalse(settings.AllowsSender("person@fakeanother.invalid"));
        Assert.IsFalse(settings.AllowsSender("person@sub.another.invalid"));
    }

    [TestMethod]
    public async Task CheckpointResetsOnUidValidityChangeAndIsSeparateForAnotherMailbox()
    {
        var client = new Incidents();
        await Job(new Mailbox(new ContactMailboxPage(1, 100, [])), client).ExecuteAsync();
        await Job(new Mailbox(new ContactMailboxPage(2, 3, [])), client).ExecuteAsync();
        var checkpoint = await _db.ContactMailboxCheckpoints.AsNoTracking().SingleAsync();
        Assert.AreEqual(2L, checkpoint.UidValidity);
        Assert.AreEqual(3L, checkpoint.LastUid);
        _configuration[$"GlobalSettings:{SettingsMap.ContactMailUsername}"] = "different@example.invalid";
        await Job(new Mailbox(new ContactMailboxPage(1, 2, [])), client).ExecuteAsync();
        Assert.AreEqual(2, await _db.ContactMailboxCheckpoints.CountAsync());
    }

    [TestMethod]
    public async Task OneMitigationFailureDoesNotBlockOtherCustomers()
    {
        var first = Contact(_now.AddHours(-1));
        var second = Contact(_now.AddHours(-1));
        second.Email = "second@outside.invalid";
        _db.Contacts.AddRange(first, second);
        await _db.SaveChangesAsync();
        var email = Email() with { Recipients = [first.Email, second.Email] };
        var client = new Incidents();
        client.FailIds.Add(first.AlertCenterIncidentId!.Value);
        var mailbox = new Mailbox(new ContactMailboxPage(1, 5, [email]));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Job(mailbox, client).ExecuteAsync());
        Assert.AreEqual(second.AlertCenterIncidentId, client.Mitigated.Single());
        client.FailIds.Clear();
        await Job(mailbox, client).ExecuteAsync();
        Assert.AreEqual(2, client.Mitigated.Count);
    }

    [TestMethod]
    public async Task MailArrivingDuringPollingIsNotSkippedWhenCheckpointAdvances()
    {
        _db.Contacts.Add(Contact(_now.AddHours(-1)));
        await _db.SaveChangesAsync();
        var mailbox = new Mailbox
        {
            LivePage = () => new ContactMailboxPage(1, 5,
                [Email() with { SentAtUtc = DateTime.UtcNow, ReceivedAtUtc = DateTime.UtcNow }])
        };
        var client = new Incidents();
        await Job(mailbox, client).ExecuteAsync();
        Assert.AreEqual(1, client.Mitigated.Count);
    }

    private ContactFollowUpJob Job(IContactMailbox mailbox, IContactIncidentClient client)
    {
        var global = new GlobalSettingsService(_db, _configuration, null!, _cache,
            new EphemeralDataProtectionProvider(), NullLogger<GlobalSettingsService>.Instance);
        return new ContactFollowUpJob(_db, new ContactMailSettingsProvider(global), new AlertCenterSettingsProvider(global),
            mailbox, client, NullLogger<ContactFollowUpJob>.Instance);
    }

    private static Contact Contact(DateTime created) => new()
    {
        Email = "customer@outside.invalid", CreateTime = created,
        AlertCenterReportId = Guid.NewGuid(), AlertCenterIncidentId = Guid.NewGuid()
    };

    private FollowUpEmail Email() => new("1:5", _now, _now, ["staff@example.invalid"], [], ["customer@outside.invalid"], false);

    private sealed class Mailbox(params ContactMailboxPage[] pages) : IContactMailbox
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public long LastCheckpoint { get; private set; }
        public Func<ContactMailboxPage>? LivePage { get; init; }
        public async IAsyncEnumerable<ContactMailboxPage> ReadAsync(ContactMailSettings settings, ContactMailboxCheckpoint checkpoint, DateTime sinceUtc)
        {
            Calls++;
            LastCheckpoint = checkpoint.LastUid;
            if (Fail) throw new IOException("Simulated mailbox outage");
            await Task.CompletedTask;
            if (LivePage != null) yield return LivePage();
            foreach (var page in pages)
                if (checkpoint.UidValidity != page.UidValidity || checkpoint.LastUid < page.LastUid) yield return page;
        }
    }

    private sealed class Incidents : IContactIncidentClient
    {
        public bool Fail { get; set; }
        public List<Guid> Mitigated { get; } = [];
        public HashSet<Guid> FailIds { get; } = [];
        public Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request) => throw new InvalidOperationException("Unexpected creation.");
        public Task MitigateAsync(Guid incidentId)
        {
            if (Fail || FailIds.Contains(incidentId)) throw new HttpRequestException("Simulated mitigation failure");
            Mitigated.Add(incidentId);
            return Task.CompletedTask;
        }
    }

    private sealed class SaveFailure : SaveChangesInterceptor
    {
        public bool FailNext { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!FailNext) return base.SavingChangesAsync(eventData, result, cancellationToken);
            FailNext = false;
            throw new DbUpdateException("Simulated save failure");
        }
    }
}
