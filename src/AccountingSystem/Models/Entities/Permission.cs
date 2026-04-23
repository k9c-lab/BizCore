using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Permission
{
    public int PermissionId { get; set; }

    [Required]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Module { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
