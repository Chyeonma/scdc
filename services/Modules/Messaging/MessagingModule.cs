using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Services;

namespace SCDC.Modules.Messaging;

public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        }

        services.AddDbContext<MessagingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDirectConversationService, DirectConversationService>();
        services.AddSingleton<IModuleDescriptor, MessagingModuleDescriptor>();
        return services;
    }

    private sealed class MessagingModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Messaging";
        public string DatabaseSchema => "messaging";
        public ModuleStage Stage => ModuleStage.Foundation;
    }
}
