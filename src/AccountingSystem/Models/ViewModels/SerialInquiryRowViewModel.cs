namespace BizCore.Models.ViewModels;

public class SerialInquiryRowViewModel
{
    public int SerialId { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string CurrentCustomerName { get; set; } = string.Empty;
    public int? InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public DateTime? SupplierWarrantyStartDate { get; set; }
    public DateTime? SupplierWarrantyEndDate { get; set; }
    public DateTime? CustomerWarrantyStartDate { get; set; }
    public DateTime? CustomerWarrantyEndDate { get; set; }
    public bool CanCustomerClaim { get; set; }
    public string CustomerClaimBlockedReason { get; set; } = string.Empty;
}
