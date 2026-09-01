using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;

namespace SCDC.Modules.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        services.AddSingleton<IModuleDescriptor, CommunityModuleDescriptor>();
        return services;
    }

    private sealed class CommunityModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Community";
        public string DatabaseSchema => "community";
        public ModuleStage Stage => ModuleStage.Foundation;
    }
}
