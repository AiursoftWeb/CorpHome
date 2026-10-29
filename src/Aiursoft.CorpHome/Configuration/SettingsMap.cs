using Aiursoft.CorpHome.Models;

namespace Aiursoft.CorpHome.Configuration;

public class SettingsMap
{
    public const string AlertCenterEnabled = "AlertCenter_Enabled";
    public const string AlertCenterEndpoint = "AlertCenter_Endpoint";
    public const string AlertCenterApiKey = "AlertCenter_ApiKey";
    public const string AlertCenterPublicBaseUrl = "AlertCenter_PublicBaseUrl";
    public const string ContactMailEnabled = "ContactMail_Enabled";
    public const string ContactMailHost = "ContactMail_Host";
    public const string ContactMailPort = "ContactMail_Port";
    public const string ContactMailUsername = "ContactMail_Username";
    public const string ContactMailPassword = "ContactMail_Password";
    public const string ContactMailFolder = "ContactMail_Folder";
    public const string ContactMailStaffSenders = "ContactMail_StaffSenders";
    public const string ContactMailFollowUpDays = "ContactMail_FollowUpDays";

    public const string ProjectName = "ProjectName";
    public const string BrandName = "BrandName";
    public const string BrandHomeUrl = "BrandHomeUrl";
    public const string ProjectLogo = "ProjectLogo";
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string Icp = "Icp";
    public const string CompanyNameZhCn = "CompanyNameZhCn";
    public const string ShowVoxihostPartnership = "ShowVoxihostPartnership";
    public const string ShowPartnerMoog = "ShowPartnerMoog";
    public const string ShowPartnerSenparc = "ShowPartnerSenparc";
    public const string ShowPartnerVoxihost = "ShowPartnerVoxihost";
    public const string ShowPartnerChopinsight = "ShowPartnerChopinsight";
    public const string ShowPartnerEgret = "ShowPartnerEgret";
    public const string ShowPartnerAoji = "ShowPartnerAoji";
    public const string BaiduKey = "BaiduKey";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = ContactMailEnabled,
            Name = Localizer["Automatically mitigate contacts after email follow-up"],
            Description = Localizer["Read a dedicated IMAPS mailbox receiving staff follow-up copies. The mail server must restrict delivery to authenticated internal staff. Requires AlertCenter integration."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailHost,
            Name = Localizer["Contact follow-up IMAP host"],
            Description = Localizer["IMAP hostname. Implicit TLS and valid server certificates are required."],
            Type = SettingType.Text,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailPort,
            Name = Localizer["Contact follow-up IMAP port"],
            Description = Localizer["Implicit TLS port, normally 993."],
            Type = SettingType.Number,
            DefaultValue = "993"
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailUsername,
            Name = Localizer["Contact follow-up mailbox username"],
            Description = Localizer["Dedicated mailbox included in your staff CC group, or explicitly copied on customer replies."],
            Type = SettingType.Text,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailPassword,
            Name = Localizer["Contact follow-up mailbox password"],
            Description = Localizer["Mailbox password or application password. No mail server administrator credentials are needed."],
            Type = SettingType.Secret,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailFolder,
            Name = Localizer["Contact follow-up mailbox folder"],
            Description = Localizer["IMAP folder to inspect. Messages are never marked as read, moved or deleted."],
            Type = SettingType.Text,
            DefaultValue = "INBOX"
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailStaffSenders,
            Name = Localizer["Allowed contact follow-up senders"],
            Description = Localizer["Comma-separated staff email addresses or @domains. This filter does not authenticate senders; restrict mailbox delivery on the mail server."],
            Type = SettingType.Text,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = ContactMailFollowUpDays,
            Name = Localizer["Contact follow-up window in days"],
            Description = Localizer["A staff email mitigates all matching earlier submissions within this window (1–30 days). Later submissions require a new follow-up."],
            Type = SettingType.Number,
            DefaultValue = "7"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterEnabled,
            Name = Localizer["Enable AlertCenter integration"],
            Description = Localizer["Send new contact submissions to AlertCenter as sev2 incidents. Disabling pauses delivery; pending submissions are retained."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterEndpoint,
            Name = Localizer["AlertCenter endpoint"],
            Description = Localizer["The HTTP(S) base URL of AlertCenter."],
            Type = SettingType.Text,
            DefaultValue = "https://alertcenter.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterApiKey,
            Name = Localizer["AlertCenter application API key"],
            Description = Localizer["A dedicated CorpHome application key with create incident permission; email follow-up also requires query and mitigate permissions."],
            Type = SettingType.Secret,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterPublicBaseUrl,
            Name = Localizer["CorpHome public URL"],
            Description = Localizer["The public HTTP(S) base URL used for contact follow-up links."],
            Type = SettingType.Text,
            DefaultValue = "https://www.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectName,
            Name = Localizer["Project Name"],
            Description = Localizer["The name of the project displayed in the frontend."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft Corporation"
        },
        new GlobalSettingDefinition
        {
            Key = BrandName,
            Name = Localizer["Brand Name"],
            Description = Localizer["The brand name displayed in the footer."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft Corporation"
        },
        new GlobalSettingDefinition
        {
            Key = BrandHomeUrl,
            Name = Localizer["Brand Home URL"],
            Description = Localizer[" The link to the brand's home page."],
            Type = SettingType.Text,
            DefaultValue = "https://www.aiursoft.com/"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectLogo,
            Name = Localizer["Project Logo"],
            Description = Localizer["The logo of the project displayed in the navbar and footer. Support jpg, png, svg."],
            Type = SettingType.File,
            DefaultValue = "",
            Subfolder = "project-logo",
            AllowedExtensions = "jpg png svg",
            MaxSizeInMb = 5
        },
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = Localizer["Allow User Adjust Nickname"],
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = CompanyNameZhCn,
            Name = Localizer["Company Name (Simplified Chinese)"],
            Description = Localizer["The company name displayed when the page language is Simplified Chinese. Leave empty to use the default brand name."],
            Type = SettingType.Text,
            DefaultValue = "苏州艾软科技有限公司"
        },
        new GlobalSettingDefinition
        {
            Key = Icp,
            Name = Localizer["ICP Number"],
            Description = Localizer["The ICP license number for China mainland users. Leave empty to hide."],
            Type = SettingType.Text,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = ShowVoxihostPartnership,
            Name = Localizer["Show Voxihost Partnership"],
            Description = Localizer["Show the Voxihost partnership section on the home page."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerMoog,
            Name = Localizer["Show Partner: Moog"],
            Description = Localizer["Show Moog in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerSenparc,
            Name = Localizer["Show Partner: Senparc"],
            Description = Localizer["Show Senparc in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerVoxihost,
            Name = Localizer["Show Partner: Voxihost"],
            Description = Localizer["Show Voxihost in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerChopinsight,
            Name = Localizer["Show Partner: Chopinsight"],
            Description = Localizer["Show Chopinsight in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerEgret,
            Name = Localizer["Show Partner: Egret"],
            Description = Localizer["Show Egret in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = ShowPartnerAoji,
            Name = Localizer["Show Partner: Aoji Airlines"],
            Description = Localizer["Show Aoji Airlines in the trusted partners section."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = BaiduKey,
            Name = Localizer["Baidu Verification Key"],
            Description = Localizer["The verification key for Baidu site verification. Will be added as <meta name=\"baidu-site-verification\" content=\"...\" /> in the page head."],
            Type = SettingType.Text,
            DefaultValue = ""
        }
    };
}
