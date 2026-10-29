using System.ComponentModel.DataAnnotations;
using System.Text;
using Aiursoft.CorpHome.Configuration;
using Aiursoft.CorpHome.Entities;
using Aiursoft.CorpHome.Services.ContactAlerts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.CorpHome.Tests;

[TestClass]
public class ContactIncidentReportTests
{
    [TestMethod]
    public void ReportIncludesActionableCustomerInformationAndStructuredContext()
    {
        var contact = Contact();
        var request = ContactIncidentReport.Create(contact, new AlertCenterSettings
        {
            PublicBaseUrl = "https://example.invalid/company/", ApiKey = "do-not-include-api-key"
        }, new ContactMailSettings { Enabled = true, FollowUpDays = 7, Password = "do-not-include-mail-password" });

        Assert.AreEqual("【CorpHome】New Consumer Jane Doe From Example Company", request.Title);
        Assert.IsNotNull(request.Content);
        Assert.Contains("Email: jane@example.invalid", request.Content);
        Assert.Contains("Organization size: 10–50", request.Content);
        Assert.Contains("Open-source usage: We use open source", request.Content);
        Assert.Contains("Deployment support", request.Content);
        Assert.Contains("7 days are automatically mitigated", request.Content);
        Assert.Contains("2026-10-27 13:29:51 UTC", request.Content);
        Assert.Contains("https://example.invalid/company/Contacts/Details/42", request.Content);
        Assert.AreEqual(42, request.Context!["contactId"]!.Value<int>());
        Assert.AreEqual("jane@example.invalid", request.Context["customer"]!["email"]!.Value<string>());
        Assert.AreEqual(contact.ServicesProvided, request.Context["servicesProvided"]!.Value<string>());
        Assert.IsTrue(request.Context["consent"]!["privacy"]!.Value<bool>());
        Assert.AreEqual(contact.AlertCenterReportId, request.ReportId);
        Assert.AreEqual("42", request.Metadata!["contactId"]!.Value<string>());
        var json = JsonConvert.SerializeObject(request);
        Assert.DoesNotContain("do-not-include", json);
        Assert.DoesNotContain("resolution are manual", request.Content);
    }

    [TestMethod]
    public void LongUnicodeInputFitsSdkAndServerLimitsWhileKeepingDetailsLink()
    {
        var contact = Contact();
        var longValue = string.Concat(Enumerable.Repeat("客户😀\n", 4000));
        contact.FirstName = contact.LastName = contact.Company = contact.JobTitle = contact.Email =
            contact.OrganizationSize = contact.ConsumeOpenSource = contact.ServicesProvided = longValue;
        var request = ContactIncidentReport.Create(contact,
            new AlertCenterSettings { PublicBaseUrl = "https://example.invalid/" + new string('a', 970) },
            new ContactMailSettings { Enabled = true, FollowUpDays = 30 });
        Assert.IsTrue(request.Title.Length <= 200);
        Assert.DoesNotContain("\n", request.Title);
        Assert.IsTrue(request.Content!.Length <= 4000);
        Assert.IsTrue(request.Content.EndsWith("/Contacts/Details/42"));
        Assert.IsTrue(request.Context!["servicesProvided"]!.Value<string>()!.EndsWith('…'));
        var results = new List<ValidationResult>();
        Assert.IsTrue(Validator.TryValidateObject(request, new ValidationContext(request), results, true),
            string.Join(", ", results.Select(result => result.ErrorMessage)));
        var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
        Assert.IsTrue(Encoding.UTF8.GetByteCount(json) < 65536, "The whole request must fit AlertCenter's 64 KiB request limit.");
    }

    [TestMethod]
    public void DisabledFollowUpDoesNotPromiseAutomaticMitigationAndEmptyFieldsAreOmitted()
    {
        var contact = Contact();
        contact.Company = contact.JobTitle = contact.FirstName = contact.LastName = string.Empty;
        var request = ContactIncidentReport.Create(contact, new AlertCenterSettings { PublicBaseUrl = "https://example.invalid" },
            new ContactMailSettings());
        Assert.AreEqual("【CorpHome】New Consumer", request.Title);
        Assert.IsNotNull(request.Content);
        Assert.DoesNotContain("Company:", request.Content);
        Assert.DoesNotContain("Name:", request.Content);
        Assert.Contains("Automatic email follow-up detection is not enabled", request.Content);
        Assert.IsFalse(request.Context!["followUp"]!["automaticMitigationEnabled"]!.Value<bool>());
    }

    private static Contact Contact() => new()
    {
        Id = 42, AlertCenterReportId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe",
        Company = "Example Company", Email = "jane@example.invalid", JobTitle = "Engineer", OrganizationSize = "10–50",
        ConsumeOpenSource = "We use open source", ServicesProvided = "Deployment support", AgreeToPrivacy = true,
        CreateTime = new DateTime(2026, 10, 27, 13, 29, 51, DateTimeKind.Utc)
    };
}
