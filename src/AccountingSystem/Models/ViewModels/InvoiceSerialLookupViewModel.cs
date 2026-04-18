namespace BizCore.Models.ViewModels;

public class InvoiceSerialLookupViewModel
{
    public int SerialId { get; set; }
    public int ItemId { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}
