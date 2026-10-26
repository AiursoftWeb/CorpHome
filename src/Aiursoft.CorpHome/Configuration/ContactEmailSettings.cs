using System.Net.Mail;

namespace Aiursoft.CorpHome.Configuration;

public sealed class ContactEmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool RequireTls { get; set; } = true;
    public Dictionary<string, string> EngineerAddresses { get; set; } = new();

    public bool IsConfigured() => !string.IsNullOrWhiteSpace(Host) && Port is >= 1 and <= 65535 &&
        IsAddress(From) && string.IsNullOrWhiteSpace(UserName) == string.IsNullOrWhiteSpace(Password);

    public static bool IsAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320 || value.Any(char.IsControl)) return false;
        return MailAddress.TryCreate(value, out var address) &&
            string.Equals(value, address.Address, StringComparison.OrdinalIgnoreCase);
    }
}
