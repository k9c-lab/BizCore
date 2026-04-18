namespace BizCore.Models.Entities;

public class InvoiceSerial
{
    public int InvoiceSerialId { get; set; }
    public int InvoiceDetailId { get; set; }
    public int SerialId { get; set; }

    public InvoiceDetail? InvoiceDetail { get; set; }
    public SerialNumber? SerialNumber { get; set; }
}
