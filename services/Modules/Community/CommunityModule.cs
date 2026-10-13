using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.Modules.Community.Features.Servers.Application;
using SCDC.Modules.Community.Features.Memberships.Application;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;
using SCDC.Modules.Community.Features.Channels.Application;
using SCDC.Contracts.Community;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Modules.Community.Infrastructure.Paging;

namespace SCDC.Modules.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var section = configuration.GetSection(CommunityOptions.SectionName);
        services.AddOptions<CommunityOptions>().Bind(section)
            .Validate(value => !string.IsNullOrWhiteSpace(value.KeyRingPath) && Path.IsPathFullyQualified(value.KeyRingPath), "Modules:Community:KeyRingPath must be an absolute persistent directory.")
            .Validate(value => value.Operations.IsValid(), "Community HMAC keys must be base64 secrets containing at least 32 bytes; configure an active key.")
            .ValidateOnStart();
        services.AddDataProtection().SetApplicationName($"SCDC.Community.{environmentName}");
        services.AddOptions<KeyManagementOptions>().Configure<IOptions<CommunityOptions>, ILoggerFactory>((keys, settings, logging) =>
            keys.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(settings.Value.KeyRingPath), logging));
        services.AddSingleton(provider => NpgsqlDataSource.Create(provider.GetRequiredService<IConfiguration>().GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database must be configured.")));
        services.AddSingleton<RelationalWorkScopeFactory>();
        services.AddSingleton<TransactionalOutbox>();
        services.AddSingleton<ServerReader>();
        services.AddSingleton<ServerCursorCodec>();
        services.AddSingleton<SearchCursorCodec>();
        services.AddSingleton<ConfigurationCursorCodec>();
        services.AddSingleton<RoleReader>();
        services.AddSingleton<ChannelReader>();
        services.AddScoped<CommunityChannelGuard>();
        services.AddScoped<IChannelAccessGuard>(provider => provider.GetRequiredService<CommunityChannelGuard>());
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<CommunityManagementGuard>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddSingleton<IModuleDescriptor, CommunityModuleDescriptor>();
        return services;
    }

    private sealed class CommunityModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Community";
        public string DatabaseSchema => "community";
        public ModuleStage Stage => ModuleStage.Active;
    }
}
