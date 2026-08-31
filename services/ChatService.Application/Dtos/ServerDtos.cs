using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public sealed class CreateServerRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

public sealed class AddMemberRequest
{
    [Required, StringLength(32, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;
}

public sealed record ServerResponse(
    Guid Id,
    string Name,
    Guid OwnerId,
    string Role,
    DateTimeOffset CreatedAt);

public sealed record MemberResponse(PublicUserResponse User, string Role, DateTimeOffset JoinedAt);
