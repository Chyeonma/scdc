using ChatService.Dtos;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[Authorize]
[Route("api/v1/messages")]
public sealed class MessagesController(IMessageService messageService) : ApiControllerBase
{
    [HttpPatch("{messageId:guid}")]
    public Task<IActionResult> Update(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(messageService.UpdateAsync(messageId, request, cancellationToken));

    [HttpDelete("{messageId:guid}")]
    public Task<IActionResult> Delete(
        Guid messageId,
        CancellationToken cancellationToken) =>
        HandleAsync(messageService.DeleteAsync(messageId, cancellationToken));
}
