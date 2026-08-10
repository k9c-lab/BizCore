using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PatientVisitItem
{
    public int PatientVisitItemId { get; set; }
    public int PatientVisitId { get; set; }
    public int ItemId { get; set; }

    [Range(0.01, 9999999)]
    [Display(Name = "จำนวน")]
    public decimal Quantity { get; set; } = 1;

    public PatientVisit? PatientVisit { get; set; }
    public Item? Item { get; set; }
}
