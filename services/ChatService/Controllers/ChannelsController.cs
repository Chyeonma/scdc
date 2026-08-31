using ChatService.Dtos;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatService.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class ChannelsController(IChannelService channelService) : ApiControllerBase
{
    [HttpGet("servers/{serverId:guid}/channels")]
    public Task<IActionResult> List(
        Guid serverId,
        CancellationToken cancellationToken) =>
        HandleAsync(channelService.ListAsync(serverId, cancellationToken));

    [HttpPost("servers/{serverId:guid}/channels")]
    public Task<IActionResult> Create(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(channelService.CreateAsync(serverId, request, cancellationToken));

    [HttpGet("channels/{channelId:guid}/messages")]
    public Task<IActionResult> GetMessages(
        Guid channelId,
        [FromQuery] string? before,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        HandleAsync(channelService.GetMessagesAsync(
            channelId,
            before,
            limit,
            cancellationToken));

    [HttpPost("channels/{channelId:guid}/messages")]
    [EnableRateLimiting("send-message")]
    public Task<IActionResult> SendMessage(
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(channelService.SendMessageAsync(channelId, request, cancellationToken));
}
