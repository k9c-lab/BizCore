namespace BizCore.Models.Entities;

public class CashSaleSerial
{
    public int CashSaleSerialId { get; set; }
    public int CashSaleDetailId { get; set; }
    public int SerialId { get; set; }

    public CashSaleDetail? CashSaleDetail { get; set; }
    public SerialNumber? SerialNumber { get; set; }
}
