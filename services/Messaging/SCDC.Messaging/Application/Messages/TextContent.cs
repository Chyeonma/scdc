using System.Text;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

// Unicode 17.0.0 ranges pinned by docs/fixtures/text-policy.json.
public static class TextContent
{
    private static readonly (int Start, int End)[] BlankRanges = [
        (0x9, 0xD),
        (0x20, 0x20),
        (0x85, 0x85),
        (0xA0, 0xA0),
        (0x1680, 0x1680),
        (0x2000, 0x200A),
        (0x2028, 0x2029),
        (0x202F, 0x202F),
        (0x205F, 0x205F),
        (0x3000, 0x3000),
        (0xAD, 0xAD),
        (0x34F, 0x34F),
        (0x61C, 0x61C),
        (0x115F, 0x1160),
        (0x17B4, 0x17B5),
        (0x180B, 0x180D),
        (0x180E, 0x180E),
        (0x180F, 0x180F),
        (0x200B, 0x200F),
        (0x202A, 0x202E),
        (0x2060, 0x2064),
        (0x2065, 0x2065),
        (0x2066, 0x206F),
        (0x3164, 0x3164),
        (0xFE00, 0xFE0F),
        (0xFEFF, 0xFEFF),
        (0xFFA0, 0xFFA0),
        (0xFFF0, 0xFFF8),
        (0x1BCA0, 0x1BCA3),
        (0x1D173, 0x1D17A),
        (0xE0000, 0xE0000),
        (0xE0001, 0xE0001),
        (0xE0002, 0xE001F),
        (0xE0020, 0xE007F),
        (0xE0080, 0xE00FF),
        (0xE0100, 0xE01EF),
        (0xE01F0, 0xE0FFF),
        (0x0, 0x1F),
        (0x7F, 0x9F),
    ];
    public static (string? Content, Error? Error) Validate(string? input)
    {
        if (input is null) return (null, Error.Validation("CONTENT_EMPTY", "Enter message text."));
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '\0' || char.IsLowSurrogate(c)) return Invalid();
            if (char.IsHighSurrogate(c))
            {
                if (++i == input.Length || !char.IsLowSurrogate(input[i])) return Invalid();
            }
        }
        var content = input.Replace("\r\n", "\n").Replace('\r', '\n');
        if (content.Length > 2000) return (null, Error.Validation("CONTENT_TOO_LONG", "Text must be at most 2000 UTF-16 units."));
        if (!content.EnumerateRunes().Any(r => !BlankRanges.Any(range => r.Value >= range.Start && r.Value <= range.End)))
            return (null, Error.Validation("CONTENT_EMPTY", "Enter visible message text."));
        return (content, null);
    }
    private static (string?, Error?) Invalid() => (null, Error.Validation("CONTENT_INVALID", "Text contains invalid Unicode or NUL."));
}
