using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public abstract class CrudControllerBase : Controller
{
    private static readonly Regex TrailingDigitsRegex = new(@"\d+$", RegexOptions.Compiled);

    protected static bool IsDuplicateConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
            || exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    protected static string Format4DigitCode(int sequence)
    {
        return Math.Clamp(sequence, 1, 9999).ToString("D4");
    }

    protected static string FormatPrefixedCode(string prefix, int sequence)
    {
        return $"{prefix}-{Format4DigitCode(sequence)}";
    }

    protected static string FormatPeriodPrefixedCode(string prefix, DateTime date, int sequence)
    {
        return $"{prefix}-{date:yyyyMM}-{Format4DigitCode(sequence)}";
    }

    protected static int ExtractSequence(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return 0;
        }

        var match = TrailingDigitsRegex.Match(code.Trim());
        return match.Success && int.TryParse(match.Value, out var sequence)
            ? sequence
            : 0;
    }
}
