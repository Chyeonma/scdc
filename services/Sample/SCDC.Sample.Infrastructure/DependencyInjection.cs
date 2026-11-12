using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SCDC.Sample.Application.Health;
using SCDC.Sample.Infrastructure.Health;

namespace SCDC.Sample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSampleInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IHealthProbe, ProcessHealthProbe>();
        return services;
    }
}
