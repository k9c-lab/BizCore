namespace BizCore.Models.ViewModels;

public class QuotationSalespersonLookupViewModel
{
    public int SalespersonId { get; set; }
    public string SalespersonName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
