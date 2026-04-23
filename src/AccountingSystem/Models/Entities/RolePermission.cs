using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class RolePermission
{
    public int RolePermissionId { get; set; }

    [Required]
    [StringLength(30)]
    public string RoleName { get; set; } = string.Empty;

    public int PermissionId { get; set; }

    public Permission? Permission { get; set; }
}
