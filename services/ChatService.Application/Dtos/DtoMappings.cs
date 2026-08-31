using ChatService.Domain.Entities;

namespace ChatService.Dtos;

internal static class DtoMappings
{
    public static UserResponse ToCurrentUser(User user) =>
        new(user.Id, user.Email, user.Username, user.DisplayName, user.CreatedAt);

    public static PublicUserResponse ToPublicUser(User user) =>
        new(user.Id, user.Username, user.DisplayName, user.CreatedAt);

    public static MessageResponse ToMessage(ChatMessage message, User author) =>
        new(
            message.Id,
            message.ChannelId,
            new MessageAuthorResponse(author.Id, author.Username, author.DisplayName),
            message.Content,
            message.Version,
            message.CreatedAt,
            message.EditedAt);
}
