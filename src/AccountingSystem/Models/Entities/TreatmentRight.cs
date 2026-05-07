using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class TreatmentRight
{
    public int TreatmentRightId { get; set; }

    [Required]
    [Display(Name = "รหัสสิทธิการรักษา")]
    [StringLength(30)]
    public string TreatmentRightCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อสิทธิการรักษา")]
    [StringLength(200)]
    public string TreatmentRightName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
