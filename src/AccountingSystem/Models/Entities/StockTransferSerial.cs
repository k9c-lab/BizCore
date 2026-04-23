namespace BizCore.Models.Entities;

public class StockTransferSerial
{
    public int StockTransferSerialId { get; set; }

    public int StockTransferDetailId { get; set; }

    public int SerialId { get; set; }

    public StockTransferDetail? StockTransferDetail { get; set; }
    public SerialNumber? SerialNumber { get; set; }
}
