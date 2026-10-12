using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Services;
using SCDC.Modules.Messaging.Realtime;

namespace SCDC.Modules.Messaging;

public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        services.AddDbContext<MessagingDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<ISharedDatabaseSession>().Connection));
        services.AddScoped<IDirectConversationService, DirectConversationService>();
        services.AddScoped<IConversationInbox, ConversationInbox>();
        services.AddScoped<ITextMessageSender, TextMessageSender>();
        services.AddScoped<IMessageHistory, MessageHistory>();
        services.AddScoped<IMessageMutator, MessageMutator>();
        services.AddSingleton<SCDC.Modules.Messaging.Infrastructure.Security.MessageFingerprint>();
        services.AddSignalR(options => options.EnableDetailedErrors = false);
        services.AddSingleton<ConversationRegistry>();
        services.AddScoped<ConversationAccess>();
        services.AddScoped<OutboxStore>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddHostedService(provider => provider.GetRequiredService<OutboxDispatcher>());
        services.AddHostedService<SubscriptionReconciler>();
        services.AddSingleton<IModuleDescriptor, MessagingModuleDescriptor>();
        return services;
    }

    private sealed class MessagingModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Messaging";
        public string DatabaseSchema => "messaging";
        public ModuleStage Stage => ModuleStage.Active;
    }
}
