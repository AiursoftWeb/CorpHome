using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aiursoft.CorpHome.Services;
using MimeKit;

namespace Aiursoft.CorpHome.Configuration;

public sealed class ContactMailSettings
{
    public bool Enabled { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 993;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Folder { get; init; } = "INBOX";
    public string[] StaffSenders { get; init; } = [];
    public int FollowUpDays { get; init; } = 7;

    public string MailboxId => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join('\n', Host.ToLowerInvariant(), Port.ToString(CultureInfo.InvariantCulture), Username, Folder))));

    public void Validate()
    {
        if (Uri.CheckHostName(Host) == UriHostNameType.Unknown || Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Folder))
            throw new InvalidOperationException("A valid contact follow-up IMAPS host, port, username, password and folder are required.");
        if (FollowUpDays is < 1 or > 30)
            throw new InvalidOperationException("The contact follow-up window must be between 1 and 30 days.");
        if (StaffSenders.Length == 0 || StaffSenders.Any(sender => sender.StartsWith('@')
                ? Uri.CheckHostName(sender[1..]) != UriHostNameType.Dns
                : !MailboxAddress.TryParse(sender, out var address) || address.Address != sender))
            throw new InvalidOperationException("Allowed follow-up senders must be email addresses or @domains.");
    }

    public bool AllowsSender(string address) => StaffSenders.Any(sender => sender.StartsWith('@')
        ? address.EndsWith(sender, StringComparison.OrdinalIgnoreCase)
        : address.Equals(sender, StringComparison.OrdinalIgnoreCase));
}

public sealed class ContactMailSettingsProvider(GlobalSettingsService settings)
{
    public async Task<ContactMailSettings> GetAsync() => new()
    {
        Enabled = await settings.GetBoolSettingAsync(SettingsMap.ContactMailEnabled),
        Host = (await settings.GetSettingValueAsync(SettingsMap.ContactMailHost)).Trim(),
        Port = await settings.GetIntSettingAsync(SettingsMap.ContactMailPort),
        Username = (await settings.GetSettingValueAsync(SettingsMap.ContactMailUsername)).Trim(),
        Password = await settings.GetSettingValueAsync(SettingsMap.ContactMailPassword),
        Folder = await settings.GetSettingValueAsync(SettingsMap.ContactMailFolder),
        StaffSenders = (await settings.GetSettingValueAsync(SettingsMap.ContactMailStaffSenders))
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
        FollowUpDays = await settings.GetIntSettingAsync(SettingsMap.ContactMailFollowUpDays)
    };
}
