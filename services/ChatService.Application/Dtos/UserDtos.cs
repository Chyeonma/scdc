using System.ComponentModel.DataAnnotations;

namespace ChatService.Dtos;

public sealed class UpdateUserRequest
{
    [Required, StringLength(64, MinimumLength = 1)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed record UserResponse(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    DateTimeOffset CreatedAt);

public sealed record PublicUserResponse(
    Guid Id,
    string Username,
    string DisplayName,
    DateTimeOffset CreatedAt);
