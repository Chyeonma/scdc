using ChatService.Domain.Entities;
using ChatService.Dtos;
using ChatService.Services.Abstractions;

namespace ChatService.Services;

public sealed class ServerService(
    IServerRepository servers,
    IUserRepository users,
    IChannelRepository channels,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    IChatRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider) : IServerService
{
    public async Task<ServiceResult<ItemsResponse<ServerResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var entities = await servers.ListForUserAsync(userId, cancellationToken);
        var items = entities.Select(server => ToResponse(server, userId)).ToList();

        return ServiceResult<ItemsResponse<ServerResponse>>.Ok(
            new ItemsResponse<ServerResponse>(items));
    }

    public async Task<ServiceResult<ServerResponse>> CreateAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length < 2)
        {
            return ServiceResult<ServerResponse>.Validation(
                "name",
                "Server name must contain at least two non-whitespace characters.");
        }

        var userId = currentUser.UserId;
        var now = timeProvider.GetUtcNow();
        var server = new ChatServer
        {
            Name = name,
            OwnerUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var membership = new ServerMember
        {
            ServerId = server.Id,
            UserId = userId,
            JoinedAt = now
        };
        var channel = new ChatChannel
        {
            ServerId = server.Id,
            Name = "general",
            CreatedAt = now
        };

        servers.AddAggregate(server, membership, channel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServerResponse>.Created(ToResponse(server, userId));
    }

    public async Task<ServiceResult<ServerResponse>> GetByIdAsync(
        Guid serverId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var server = await servers.FindForMemberAsync(serverId, userId, cancellationToken);

        return server is null
            ? ServiceResult<ServerResponse>.NotFound("Server not found.")
            : ServiceResult<ServerResponse>.Ok(ToResponse(server, userId));
    }

    public async Task<ServiceResult<MemberResponse>> AddMemberAsync(
        Guid serverId,
        AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!await servers.IsOwnerAsync(serverId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<MemberResponse>.NotFound(
                "Server not found or the current user is not its owner.");
        }

        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var user = await users.FindByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
        if (user is null)
        {
            return ServiceResult<MemberResponse>.NotFound("User not found.");
        }

        var now = timeProvider.GetUtcNow();
        servers.AddMember(new ServerMember { ServerId = serverId, UserId = user.Id, JoinedAt = now });
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            return ServiceResult<MemberResponse>.Conflict("Membership already exists.");
        }

        return ServiceResult<MemberResponse>.Created(
            new MemberResponse(DtoMappings.ToPublicUser(user), "member", now));
    }

    public async Task<ServiceResult<bool>> LeaveAsync(
        Guid serverId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var server = await servers.FindByIdAsync(serverId, cancellationToken);
        if (server is null)
        {
            return ServiceResult<bool>.NotFound("Server not found.");
        }

        if (server.OwnerUserId == userId)
        {
            return ServiceResult<bool>.Conflict("The server owner cannot leave the server.");
        }

        var membership = await servers.FindMembershipAsync(serverId, userId, cancellationToken);
        if (membership is null)
        {
            return ServiceResult<bool>.NotFound("Membership not found.");
        }

        var channelIds = await channels.ListIdsByServerAsync(serverId, cancellationToken);
        servers.RemoveMember(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.RevokeChannelAccessAsync(userId, channelIds, cancellationToken);

        return ServiceResult<bool>.NoContent();
    }

    private static ServerResponse ToResponse(ChatServer server, Guid userId) =>
        new(
            server.Id,
            server.Name,
            server.OwnerUserId,
            server.OwnerUserId == userId ? "owner" : "member",
            server.CreatedAt);
}
