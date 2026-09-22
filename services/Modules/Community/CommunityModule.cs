using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;
using SCDC.Contracts.Community;
using SCDC.Modules.Community.Application;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Modules.Community.Infrastructure.Services;

namespace SCDC.Modules.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<IChannelAccessChecker, ChannelAccessChecker>();
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
