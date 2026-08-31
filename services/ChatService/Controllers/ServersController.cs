using ChatService.Dtos;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[Authorize]
[Route("api/v1/servers")]
public sealed class ServersController(IServerService serverService) : ApiControllerBase
{
    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        HandleAsync(serverService.ListAsync(cancellationToken));

    [HttpPost]
    public Task<IActionResult> Create(
        CreateServerRequest request,
        CancellationToken cancellationToken) =>
        HandleCreatedAtAsync(
            serverService.CreateAsync(request, cancellationToken),
            server => CreatedAtAction(nameof(GetById), new { serverId = server.Id }, server));

    [HttpGet("{serverId:guid}")]
    public Task<IActionResult> GetById(
        Guid serverId,
        CancellationToken cancellationToken) =>
        HandleAsync(serverService.GetByIdAsync(serverId, cancellationToken));

    [HttpPost("{serverId:guid}/members")]
    public Task<IActionResult> AddMember(
        Guid serverId,
        AddMemberRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(serverService.AddMemberAsync(serverId, request, cancellationToken));

    [HttpDelete("{serverId:guid}/members/me")]
    public Task<IActionResult> Leave(
        Guid serverId,
        CancellationToken cancellationToken) =>
        HandleAsync(serverService.LeaveAsync(serverId, cancellationToken));
}
