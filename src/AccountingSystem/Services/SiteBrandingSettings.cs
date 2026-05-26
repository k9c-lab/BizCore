namespace BizCore.Services;

public class SiteBrandingSettings
{
    public string SiteName { get; set; } = "BizCore";
    public string SiteSubtitle { get; set; } = "ระบบงานหลัก";
    public string LoginTitle { get; set; } = "BizCore";
    public string LoginSubtitle { get; set; } = "ระบบงานบัญชีและปฏิบัติการ";
    public string? EnvironmentLabel { get; set; }
}
