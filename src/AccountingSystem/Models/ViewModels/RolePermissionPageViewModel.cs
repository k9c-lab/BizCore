using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class RolePermissionPageViewModel
{
    public string SelectedRole { get; set; } = "CentralAdmin";
    public IEnumerable<SelectListItem> RoleOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<RolePermissionModuleViewModel> Modules { get; set; } = Array.Empty<RolePermissionModuleViewModel>();
}
