namespace BizCore.Models.ViewModels;

public class AccountProfilePageViewModel
{
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public AccountProfileUpdateViewModel Profile { get; set; } = new();
    public AccountChangePasswordViewModel Password { get; set; } = new();
}
