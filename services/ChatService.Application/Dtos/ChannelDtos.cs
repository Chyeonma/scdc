using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public sealed class CreateChannelRequest
{
    [Required, StringLength(100, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Name { get; init; } = string.Empty;
}

public sealed record ChannelResponse(Guid Id, Guid ServerId, string Name, DateTimeOffset CreatedAt);
