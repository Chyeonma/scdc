using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SCDC.Api.OpenApi;

internal sealed class AuthenticationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];
        var allowsAnonymous = methodAttributes.OfType<AllowAnonymousAttribute>().Any()
            || controllerAttributes.OfType<AllowAnonymousAttribute>().Any();
        var requiresAuthorization = methodAttributes.OfType<AuthorizeAttribute>().Any()
            || controllerAttributes.OfType<AuthorizeAttribute>().Any();

        if (allowsAnonymous || !requiresAuthorization)
        {
            operation.Security = [];
        }
    }
}
