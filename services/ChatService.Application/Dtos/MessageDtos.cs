using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public sealed class SendMessageRequest
{
    public Guid ClientMessageId { get; init; }

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class UpdateMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed record MessageAuthorResponse(Guid Id, string Username, string DisplayName);

public sealed record MessageResponse(
    Guid Id,
    Guid ChannelId,
    MessageAuthorResponse Author,
    string Content,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt);

public sealed record MessageHistoryResponse(
    IReadOnlyList<MessageResponse> Items,
    string? NextCursor,
    bool HasMore);
