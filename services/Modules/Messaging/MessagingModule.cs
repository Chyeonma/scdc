using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Hubs;
using SCDC.Modules.Messaging.Infrastructure;
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
        services.AddOptions<MessagingOutboxOptions>()
            .Bind(configuration.GetSection(MessagingOutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.PollInterval > TimeSpan.Zero, "Outbox poll interval must be positive.")
            .Validate(options => options.InitialRetryDelay > TimeSpan.Zero, "Outbox retry delay must be positive.")
            .Validate(options => options.MaxRetryDelay >= options.InitialRetryDelay, "Outbox max retry delay must not be shorter than the initial delay.")
            .ValidateOnStart();
        services.AddScoped<IDirectConversationService, DirectConversationService>();
        services.AddScoped<IGroupConversationService, GroupConversationService>();
        services.AddSingleton<MessageRateLimiter>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IReadStateService, ReadStateService>();
        services.AddScoped<IUnreadCountReader, UnreadCountReader>();
        services.AddScoped<SpaceMessageAccess>();
        services.AddScoped<IChannelSpaceProvisioner, ChannelSpaceProvisioner>();
        services.AddScoped<IRealtimeSpaceAccess, RealtimeSpaceAccess>();
        services.AddSingleton<RealtimeConnectionRegistry>();
        services.AddSingleton<MessagingRealtimeAccessRevoker>();
        services.AddSingleton<IRealtimeAccessRevoker>(provider =>
            provider.GetRequiredService<MessagingRealtimeAccessRevoker>());
        services.AddSingleton<IRealtimeSessionRevoker>(provider =>
            provider.GetRequiredService<MessagingRealtimeAccessRevoker>());
        services.AddScoped<IRealtimeMessagePublisher, MessagingRealtimePublisher>();
        services.AddScoped<IMessagingOutboxDispatcher, MessagingOutboxDispatcher>();
        services.AddHostedService<MessagingOutboxWorker>();
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
