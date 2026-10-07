using System.Buffers;
using System.Text;
using System.Text.Json;

namespace SCDC.BuildingBlocks.Application.Text;

public static class UnicodeTextPolicy
{
    private static readonly (int Start, int End)[] Whitespace;
    private static readonly (int Start, int End)[] Ignorable;

    static UnicodeTextPolicy()
    {
        using var stream = typeof(UnicodeTextPolicy).Assembly.GetManifestResourceStream("SCDC.TextPolicy.json")!;
        using var document = JsonDocument.Parse(stream);
        Whitespace = ReadRanges(document.RootElement.GetProperty("whitespaceRanges"));
        Ignorable = ReadRanges(document.RootElement.GetProperty("defaultIgnorableRanges"));
    }

    public static bool IsValidUnicode(string value)
    {
        var text = value.AsSpan();
        while (!text.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(text, out var rune, out var consumed) != OperationStatus.Done || rune.Value == 0)
                return false;
            text = text[consumed..];
        }
        return true;
    }

    public static bool IsWhitespace(Rune rune) => InRanges(rune.Value, Whitespace);
    public static bool IsControl(Rune rune) => rune.Value <= 0x1f || rune.Value is >= 0x7f and <= 0x9f;
    public static bool IsBlank(string value) => value.EnumerateRunes().All(rune => IsWhitespace(rune)
        || IsControl(rune) || InRanges(rune.Value, Ignorable));

    public static string TrimWhitespace(string value)
    {
        var text = value.AsSpan();
        while (!text.IsEmpty && Rune.DecodeFromUtf16(text, out var first, out var size) == OperationStatus.Done && IsWhitespace(first))
            text = text[size..];
        while (!text.IsEmpty && Rune.DecodeLastFromUtf16(text, out var last, out var size) == OperationStatus.Done && IsWhitespace(last))
            text = text[..^size];
        return text.ToString();
    }

    public static bool IsValidName(string value, int minimum, int maximum) => IsValidUnicode(value)
        && value.Length >= minimum && value.Length <= maximum && !IsBlank(value)
        && value.EnumerateRunes().All(rune => !IsControl(rune) && rune.Value is not (0x2028 or 0x2029));

    public static string? NormalizeDescription(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    public static string NormalizeNameKey(string value) => TrimWhitespace(value).Normalize(NormalizationForm.FormC).ToLowerInvariant();

    private static bool InRanges(int scalar, (int Start, int End)[] ranges) => ranges.Any(range => scalar >= range.Start && scalar <= range.End);

    private static (int, int)[] ReadRanges(JsonElement ranges) => ranges.EnumerateArray().Select(entry =>
    {
        var parts = entry.GetString()!.Split('-');
        var start = Convert.ToInt32(parts[0], 16);
        return (start, parts.Length == 1 ? start : Convert.ToInt32(parts[1], 16));
    }).ToArray();
}
