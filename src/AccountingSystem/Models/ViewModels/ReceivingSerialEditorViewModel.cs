using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class ReceivingSerialEditorViewModel
{
    [Required]
    [StringLength(120)]
    public string SerialNo { get; set; } = string.Empty;
}
