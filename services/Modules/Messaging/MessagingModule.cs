using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Services;
using SCDC.Modules.Messaging.Realtime;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Authentication;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Community;

namespace SCDC.Modules.Messaging;

public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IIdentityFenceOwner>(new PostgresIdentityFenceOwner(configuration, AccessOwner.Messaging));
        services.AddSingleton<ICommunityFenceOwner>(new PostgresCommunityFenceOwner(configuration, AccessOwner.Messaging));
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Messaging")))
            throw new InvalidOperationException("ConnectionStrings:Messaging must be configured.");
        services.AddScoped(provider => new MessagingSession(ModuleDatabaseConnection.Get(provider.GetRequiredService<IConfiguration>(), "Messaging")));
        services.AddDbContext<MessagingDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<MessagingSession>().Connection));
        services.AddScoped<MessagingIdentityAccess>();
        services.AddScoped<MessagingProfiles>();
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
        services.AddScoped<IChatSpaceLifecycle, ChatSpaceLifecycle>();
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
