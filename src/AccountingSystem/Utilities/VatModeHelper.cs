namespace BizCore.Utilities;

public static class VatModeHelper
{
    public const string NoVat = "NoVAT";
    public const string VatExclusive = "VATExclusive";
    public const string VatInclusive = "VATInclusive";
    public const decimal VatRate = 0.07m;

    public static string Normalize(string? vatType, string? defaultMode = null)
    {
        if (string.Equals(vatType, NoVat, StringComparison.OrdinalIgnoreCase))
        {
            return NoVat;
        }

        if (string.Equals(vatType, VatInclusive, StringComparison.OrdinalIgnoreCase))
        {
            return VatInclusive;
        }

        if (string.Equals(vatType, VatExclusive, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vatType, "VAT", StringComparison.OrdinalIgnoreCase))
        {
            return VatExclusive;
        }

        return defaultMode ?? NoVat;
    }

    public static bool IsValid(string? vatType)
    {
        var normalized = Normalize(vatType, string.Empty);
        return normalized is NoVat or VatExclusive or VatInclusive;
    }

    public static bool IsTaxable(string? vatType)
    {
        return Normalize(vatType) is VatExclusive or VatInclusive;
    }

    public static string GetDisplayLabel(string? vatType)
    {
        return Normalize(vatType) switch
        {
            VatExclusive => "ราคายังไม่รวม VAT",
            VatInclusive => "ราคารวม VAT",
            _ => "ไม่มี VAT"
        };
    }

    // amountInDocumentPricing is the amount after discounts in the same pricing basis as the selected mode.
    public static VatComputation ComputeFromDocumentPricing(decimal amountInDocumentPricing, string? vatType)
    {
        var normalized = Normalize(vatType);
        var normalizedAmount = Math.Max(amountInDocumentPricing, 0m);

        return normalized switch
        {
            VatExclusive => ComputeExclusive(normalizedAmount),
            VatInclusive => ComputeInclusive(normalizedAmount),
            _ => new VatComputation(normalizedAmount, 0m, normalizedAmount)
        };
    }

    // subtotalInDocumentPricing and discountInDocumentPricing are before/after discount inputs in the
    // same pricing basis the user entered for the selected VAT mode.
    public static VatSummaryBreakdown ComputeSummaryBreakdown(
        decimal subtotalInDocumentPricing,
        decimal discountInDocumentPricing,
        string? vatType)
    {
        var normalized = Normalize(vatType);
        var grossSubtotal = Math.Max(subtotalInDocumentPricing, 0m);
        var grossDiscount = Math.Min(Math.Max(discountInDocumentPricing, 0m), grossSubtotal);
        var grossNet = grossSubtotal - grossDiscount;

        if (normalized == NoVat)
        {
            return new VatSummaryBreakdown(grossSubtotal, grossDiscount, grossNet, 0m, grossNet);
        }

        if (normalized == VatExclusive)
        {
            var vat = Math.Round(grossNet * VatRate, 2, MidpointRounding.AwayFromZero);
            return new VatSummaryBreakdown(grossSubtotal, grossDiscount, grossNet, vat, grossNet + vat);
        }

        var subtotalBeforeVat = Math.Round(grossSubtotal / (1m + VatRate), 2, MidpointRounding.AwayFromZero);
        var netBeforeVat = Math.Round(grossNet / (1m + VatRate), 2, MidpointRounding.AwayFromZero);
        var discountBeforeVat = subtotalBeforeVat - netBeforeVat;
        var vatAmount = grossNet - netBeforeVat;
        return new VatSummaryBreakdown(subtotalBeforeVat, discountBeforeVat, netBeforeVat, vatAmount, grossNet);
    }

    private static VatComputation ComputeExclusive(decimal amountBeforeVat)
    {
        var vatAmount = Math.Round(amountBeforeVat * VatRate, 2, MidpointRounding.AwayFromZero);
        var totalAmount = amountBeforeVat + vatAmount;
        return new VatComputation(amountBeforeVat, vatAmount, totalAmount);
    }

    private static VatComputation ComputeInclusive(decimal totalAmount)
    {
        var amountBeforeVat = Math.Round(totalAmount / (1m + VatRate), 2, MidpointRounding.AwayFromZero);
        var vatAmount = totalAmount - amountBeforeVat;
        return new VatComputation(amountBeforeVat, vatAmount, totalAmount);
    }
}

public readonly record struct VatComputation(decimal AmountBeforeVat, decimal VatAmount, decimal TotalAmount);
public readonly record struct VatSummaryBreakdown(
    decimal SubtotalBeforeVat,
    decimal DiscountBeforeVat,
    decimal NetBeforeVat,
    decimal VatAmount,
    decimal TotalAmount);
