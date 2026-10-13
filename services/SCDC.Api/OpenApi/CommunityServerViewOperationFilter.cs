using Microsoft.OpenApi;
using SCDC.Api.Controllers.Community;
using SCDC.Modules.Community.Features.Servers.Application;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SCDC.Api.OpenApi;

internal sealed class CommunityServerViewOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ServersController)
            || context.MethodInfo.Name != nameof(ServersController.Get)
            || operation.Responses is null || !operation.Responses.TryGetValue("200", out var response)
            || response.Content is null)
            return;

        var summary = context.SchemaGenerator.GenerateSchema(typeof(ServerSummary), context.SchemaRepository);
        var detail = context.SchemaGenerator.GenerateSchema(typeof(ServerDetail), context.SchemaRepository);
        foreach (var media in response.Content.Values)
            media.Schema = new OpenApiSchema { OneOf = [summary, detail] };
    }
}
