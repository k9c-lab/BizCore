using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PatientVisitAuditLog
{
    [Key]
    public int AuditLogId { get; set; }
    public int PatientVisitId { get; set; }
    public PatientVisit? PatientVisit { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}
