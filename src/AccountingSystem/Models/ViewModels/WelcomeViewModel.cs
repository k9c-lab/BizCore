namespace BizCore.Models.ViewModels;

public class WelcomeViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool CanUseDashboard { get; set; }
    public bool CanUseFinancialOverview { get; set; }
    public bool CanUseInventoryOverview { get; set; }
    public bool CanUseAnnouncements { get; set; }
    public List<WelcomeAnnouncementViewModel> Announcements { get; set; } = new();
}
