using System.Text.Json;

namespace ChatService.Common.Messaging;

internal static class MessageCursor
{
    public static string Encode(DateTimeOffset createdAt, Guid messageId)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            new CursorPayload(createdAt, messageId),
            JsonSerializerOptions.Web);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? value, out DecodedMessageCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = (base64.Length % 4) switch
            {
                2 => base64 + "==",
                3 => base64 + "=",
                _ => base64
            };
            var payload = JsonSerializer.Deserialize<CursorPayload>(
                Convert.FromBase64String(base64),
                JsonSerializerOptions.Web);
            if (payload is null || payload.MessageId == Guid.Empty)
            {
                return false;
            }

            cursor = new DecodedMessageCursor(payload.CreatedAt, payload.MessageId);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record CursorPayload(DateTimeOffset CreatedAt, Guid MessageId);
}

internal readonly record struct DecodedMessageCursor(DateTimeOffset CreatedAt, Guid MessageId);
