using Aiursoft.CorpHome.Models;

namespace Aiursoft.CorpHome.Configuration;

public class SettingsMap
{
    public const string ProjectName = "ProjectName";
    public const string BrandName = "BrandName";
    public const string BrandHomeUrl = "BrandHomeUrl";
    public const string ProjectLogo = "ProjectLogo";
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string Icp = "Icp";
    public const string ShowVoxihostPartnership = "ShowVoxihostPartnership";
    public const string ShowPartnerMoog = "ShowPartnerMoog";
    public const string ShowPartnerSenparc = "ShowPartnerSenparc";
    public const string ShowPartnerVoxihost = "ShowPartnerVoxihost";
    public const string ShowPartnerChopinsight = "ShowPartnerChopinsight";
    public const string ShowPartnerEgret = "ShowPartnerEgret";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
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
        }
    };
}
