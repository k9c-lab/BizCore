namespace BizCore.Models.ViewModels;

public class RolePermissionGroupViewModel
{
    public string Title { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public int GrantedCount { get; set; }
    public IReadOnlyList<RolePermissionItemViewModel> Permissions { get; set; } = Array.Empty<RolePermissionItemViewModel>();
}
