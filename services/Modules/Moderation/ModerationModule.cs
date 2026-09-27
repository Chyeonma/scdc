using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Moderation.Application;
using SCDC.Modules.Moderation.Infrastructure;

namespace SCDC.Modules.Moderation;

public static class ModerationModule
{
    public static IServiceCollection AddModerationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        services.AddDbContext<ModerationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMessageReportService, MessageReportService>();
        services.AddScoped<IPlatformReviewAccess, PlatformReviewAccess>();
        services.AddSingleton<IModuleDescriptor, Descriptor>();
        return services;
    }

    private sealed class Descriptor : IModuleDescriptor
    {
        public string Name => "Moderation";
        public string DatabaseSchema => "moderation";
        public ModuleStage Stage => ModuleStage.Foundation;
    }
}
