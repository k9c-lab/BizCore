namespace BizCore.Models.ViewModels;

public class RolePermissionModuleViewModel
{
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public int GrantedCount { get; set; }
    public IReadOnlyList<RolePermissionGroupViewModel> Groups { get; set; } = Array.Empty<RolePermissionGroupViewModel>();
}
