using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
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
        services.AddOptions<AttachmentStorageOptions>()
            .Bind(configuration.GetSection(AttachmentStorageOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
                && endpoint.Scheme is "http" or "https"
                && !string.IsNullOrWhiteSpace(options.AccessKey)
                && !string.IsNullOrWhiteSpace(options.SecretKey)
                && !string.IsNullOrWhiteSpace(options.Bucket)
                && !string.IsNullOrWhiteSpace(options.ClamAvHost)
                && options.ClamAvPort is > 0 and <= 65535,
                "Attachment storage endpoint, bucket, credentials, and scanner must be configured.")
            .ValidateOnStart();
        services.AddSingleton<IMinioClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AttachmentStorageOptions>>().Value;
            var endpoint = new Uri(options.Endpoint);
            var client = new MinioClient()
                .WithEndpoint(endpoint.Host, endpoint.Port)
                .WithCredentials(options.AccessKey, options.SecretKey);
            return (endpoint.Scheme == "https" ? client.WithSSL() : client).Build();
        });
        services.AddSingleton<IAttachmentObjectStore, MinioAttachmentObjectStore>();
        services.AddSingleton<IFileScanner, ClamAvFileScanner>();
        services.AddScoped<IAttachmentUploadService, AttachmentUploadService>();
        services.AddScoped<IAttachmentDownloadService, AttachmentDownloadService>();
        services.AddScoped<IAttachmentCleanupService, AttachmentCleanupService>();
        services.AddHostedService<AttachmentCleanupWorker>();
        services.AddScoped<IDirectConversationService, DirectConversationService>();
        services.AddScoped<IGroupConversationService, GroupConversationService>();
        services.AddSingleton<MessageRateLimiter>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IReadStateService, ReadStateService>();
        services.AddScoped<ISpacePreferencesService, SpacePreferencesService>();
        services.AddScoped<IUnreadCountReader, UnreadCountReader>();
        services.AddScoped<SpaceMessageAccess>();
        services.AddScoped<IChannelSpaceProvisioner, ChannelSpaceProvisioner>();
        services.AddScoped<IRealtimeSpaceAccess, RealtimeSpaceAccess>();
        services.AddScoped<ITypingAccess, TypingAccess>();
        services.AddSingleton<RealtimeConnectionRegistry>();
        services.AddSingleton<TypingStateRegistry>();
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
