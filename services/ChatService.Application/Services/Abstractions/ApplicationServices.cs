using ChatService.Dtos;

namespace ChatService.Services.Abstractions;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<TokenResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<bool>> LogoutAsync(
        RefreshRequest request,
        CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<ServiceResult<UserResponse>> GetCurrentAsync(CancellationToken cancellationToken);
    Task<ServiceResult<UserResponse>> UpdateCurrentAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken);
}

public interface IServerService
{
    Task<ServiceResult<ItemsResponse<ServerResponse>>> ListAsync(CancellationToken cancellationToken);
    Task<ServiceResult<ServerResponse>> CreateAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<ServerResponse>> GetByIdAsync(
        Guid serverId,
        CancellationToken cancellationToken);
    Task<ServiceResult<MemberResponse>> AddMemberAsync(
        Guid serverId,
        AddMemberRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<bool>> LeaveAsync(Guid serverId, CancellationToken cancellationToken);
}

public interface IChannelService
{
    Task<ServiceResult<ItemsResponse<ChannelResponse>>> ListAsync(
        Guid serverId,
        CancellationToken cancellationToken);
    Task<ServiceResult<ChannelResponse>> CreateAsync(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<MessageHistoryResponse>> GetMessagesAsync(
        Guid channelId,
        string? before,
        int limit,
        CancellationToken cancellationToken);
    Task<ServiceResult<MessageResponse>> SendMessageAsync(
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken);
}

public interface IMessageService
{
    Task<ServiceResult<MessageResponse>> UpdateAsync(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken);
}
