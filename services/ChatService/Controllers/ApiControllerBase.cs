using ChatService.Common.Errors;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> HandleAsync<T>(Task<ServiceResult<T>> operation) =>
        this.ToActionResult(await operation);

    protected async Task<IActionResult> HandleCreatedAtAsync<T>(
        Task<ServiceResult<T>> operation,
        Func<T, IActionResult> createdAt)
    {
        var result = await operation;
        return result.Status == ServiceStatus.Created && result.Value is not null
            ? createdAt(result.Value)
            : this.ToActionResult(result);
    }
}
