namespace BizCore.Models.Entities;

public class StockIssueSerial
{
    public int StockIssueSerialId { get; set; }

    public int StockIssueDetailId { get; set; }

    public int SerialId { get; set; }

    public StockIssueDetail? StockIssueDetail { get; set; }
    public SerialNumber? SerialNumber { get; set; }
}
