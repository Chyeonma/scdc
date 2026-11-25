using Microsoft.OpenApi;
using SCDC.Modules.Community.Features.Servers.Application;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SCDC.Api.OpenApi;

internal sealed class CommunityResponseSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema definition ||
            (context.Type != typeof(ServerSummary) && context.Type != typeof(ServerDetail)
            && context.Type != typeof(MembershipView) && context.Type != typeof(MyServerPage)))
            return;

        // Response fields are always present, including fields whose value may be null.
        definition.Required = definition.Properties?.Keys.ToHashSet(StringComparer.Ordinal) ?? [];
        definition.AdditionalPropertiesAllowed = false;
    }
}
