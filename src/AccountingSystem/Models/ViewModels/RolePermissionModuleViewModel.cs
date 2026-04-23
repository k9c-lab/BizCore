namespace BizCore.Models.ViewModels;

public class RolePermissionModuleViewModel
{
    public string Module { get; set; } = string.Empty;
    public IReadOnlyList<RolePermissionItemViewModel> Permissions { get; set; } = Array.Empty<RolePermissionItemViewModel>();
}
