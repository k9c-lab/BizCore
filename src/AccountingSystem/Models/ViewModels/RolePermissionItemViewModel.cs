namespace BizCore.Models.ViewModels;

public class RolePermissionItemViewModel
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string KindLabel { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
