namespace BizCore.Models.ViewModels;

public class WelcomeAnnouncementViewModel
{
    public int AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string DateRangeLabel { get; set; } = string.Empty;
    public string AccentClass { get; set; } = string.Empty;
}
