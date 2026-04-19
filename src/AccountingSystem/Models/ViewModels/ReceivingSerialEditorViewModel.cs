using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class ReceivingSerialEditorViewModel
{
    [StringLength(120)]
    public string? SerialNo { get; set; }
}
