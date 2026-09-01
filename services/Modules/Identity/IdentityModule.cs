using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;

namespace SCDC.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        services.AddSingleton<IModuleDescriptor, IdentityModuleDescriptor>();
        return services;
    }

    private sealed class IdentityModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Identity";
        public string DatabaseSchema => "identity";
        public ModuleStage Stage => ModuleStage.Foundation;
    }
}
