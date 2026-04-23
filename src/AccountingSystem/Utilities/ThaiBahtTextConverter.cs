using System;
using System.Collections.Generic;
using System.Linq;

namespace BizCore.Utilities;

public static class ThaiBahtTextConverter
{
    private static readonly string[] DigitTexts =
    {
        "ศูนย์",
        "หนึ่ง",
        "สอง",
        "สาม",
        "สี่",
        "ห้า",
        "หก",
        "เจ็ด",
        "แปด",
        "เก้า"
    };

    private static readonly string[] PositionTexts =
    {
        string.Empty,
        "สิบ",
        "ร้อย",
        "พัน",
        "หมื่น",
        "แสน",
        "ล้าน"
    };

    public static string ToText(decimal amount)
    {
        var roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (roundedAmount == 0m)
        {
            return "ศูนย์บาทถ้วน";
        }

        var absoluteAmount = Math.Abs(roundedAmount);
        var integerPart = decimal.ToInt64(decimal.Truncate(absoluteAmount));
        var satangPart = decimal.ToInt32(decimal.Round((absoluteAmount - integerPart) * 100m, 0, MidpointRounding.AwayFromZero));

        if (satangPart == 100)
        {
            integerPart += 1;
            satangPart = 0;
        }

        var bahtText = ConvertInteger(integerPart);
        var satangText = satangPart == 0 ? "ถ้วน" : $"{ConvertInteger(satangPart)}สตางค์";
        var prefix = roundedAmount < 0 ? "ลบ" : string.Empty;

        return $"{prefix}{bahtText}บาท{satangText}";
    }

    private static string ConvertInteger(long value)
    {
        if (value == 0)
        {
            return DigitTexts[0];
        }

        var parts = new List<string>();
        var groupIndex = 0;

        while (value > 0)
        {
            var groupValue = (int)(value % 1_000_000);
            if (groupValue > 0)
            {
                var groupText = ConvertBelowMillion(groupValue);
                if (groupIndex > 0)
                {
                    groupText += string.Concat(Enumerable.Repeat("ล้าน", groupIndex));
                }

                parts.Insert(0, groupText);
            }

            value /= 1_000_000;
            groupIndex++;
        }

        return string.Concat(parts);
    }

    private static string ConvertBelowMillion(int value)
    {
        if (value == 0)
        {
            return string.Empty;
        }

        var digits = value.ToString();
        var result = string.Empty;

        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[i] - '0';
            if (digit == 0)
            {
                continue;
            }

            var position = digits.Length - i - 1;
            if (position == 0)
            {
                if (digit == 1 && digits.Length > 1)
                {
                    result += "เอ็ด";
                }
                else
                {
                    result += DigitTexts[digit];
                }
            }
            else if (position == 1)
            {
                if (digit == 1)
                {
                    result += "สิบ";
                }
                else if (digit == 2)
                {
                    result += "ยี่สิบ";
                }
                else
                {
                    result += DigitTexts[digit] + PositionTexts[position];
                }
            }
            else
            {
                result += DigitTexts[digit] + PositionTexts[position];
            }
        }

        return result;
    }
}
