extern alias CommunityApi;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community;
using SCDC.Modules.Community.Features.Servers.Application;
using SCDC.Modules.Community.Features.Servers.Domain;
using CommunityProgram = CommunityApi::CommunityProgram;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class CommunityHostTests(CommunityHostFixture fixture) : IClassFixture<CommunityHostFixture>
{
    [Fact]
    public async Task Host_boots_with_only_Community_database_and_reports_preparation_instead_of_ready()
    {
        var live = await fixture.Client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        var state = JsonDocument.Parse(await live.Content.ReadAsStringAsync()).RootElement;
        Assert.False(state.GetProperty("workersEnabled").GetBoolean());
        Assert.Equal("preparation", state.GetProperty("mode").GetString());
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.GetAsync("/health/database")).StatusCode);
        await Unavailable(await fixture.Client.GetAsync("/health/ready"));
        Assert.DoesNotContain(fixture.Factory.Services.GetServices<IHostedService>(), service =>
            service.GetType().Assembly == typeof(CommunityModule).Assembly);
        var settings = fixture.Factory.Services.GetRequiredService<IConfiguration>();
        Assert.True(string.IsNullOrEmpty(settings.GetConnectionString("Messaging")));
        Assert.True(string.IsNullOrEmpty(settings.GetConnectionString("Identity")));
    }

    [Theory]
    [InlineData("GET", "/api/v1/servers")]
    [InlineData("POST", "/api/v1/servers")]
    [InlineData("GET", "/API/V1/SERVERS/search?q=test")]
    [InlineData("POST", "/api/v1/servers/11111111-1111-4111-8111-111111111111/channels")]
    [InlineData("PUT", "/api/v1/servers/11111111-1111-4111-8111-111111111111/channels/22222222-2222-4222-8222-222222222222/access")]
    public async Task Business_requests_are_blocked_before_authentication_or_owner_mutation(string method, string path)
    {
        await Unavailable(await fixture.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM community.servers"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM community.channel_provisions"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM community.access_changes"));
    }

    [Fact]
    public async Task Missing_Messaging_cannot_return_a_provision_result_or_fence_receipt()
    {
        var provision = fixture.Factory.Services.GetRequiredKeyedService<IChannelProvisioning>(AccessOwner.Community);
        var error = await Assert.ThrowsAsync<AccessCheckException>(() => provision.PrepareAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, [1]), default));
        Assert.Equal("COMMUNITY_INTEGRATIONS_NOT_CONFIGURED", error.Code);
        var owner = fixture.Factory.Services.GetServices<ICommunityFenceOwner>().Single(owner => owner.Owner == AccessOwner.Messaging);
        error = await Assert.ThrowsAsync<AccessCheckException>(() => owner.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, []), default));
        Assert.Equal("COMMUNITY_INTEGRATIONS_NOT_CONFIGURED", error.Code);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM community.fence_receipts"));
    }

    [Fact]
    public async Task Swagger_exposes_Community_without_Identity_DM_or_Hub_routes()
    {
        var response = await fixture.Client.GetAsync("/swagger/community/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("paths");
        Assert.NotEmpty(paths.EnumerateObject());
        Assert.All(paths.EnumerateObject(), path => Assert.StartsWith("/api/v1/servers", path.Name));
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null)).StatusCode);
    }

    [Fact]
    public async Task Fresh_baseline_matches_source_columns_and_contains_no_foreign_owner_dependencies()
    {
        var source = """
            SELECT table_name||':'||column_name||':'||data_type||':'||is_nullable
            FROM information_schema.columns WHERE table_schema='community' ORDER BY table_name,ordinal_position
            """;
        Assert.Equal(await fixture.Rows(fixture.AdminSource, source), await fixture.Rows(fixture.AdminTarget, source));
        Assert.Equal(21L, await fixture.Scalar("SELECT count(*) FROM pg_tables WHERE schemaname='community'"));
        Assert.Equal(0L, await fixture.Scalar("""
            SELECT count(*) FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid
            JOIN pg_namespace n ON n.oid=t.relnamespace JOIN pg_class r ON r.oid=c.confrelid
            JOIN pg_namespace rn ON rn.oid=r.relnamespace
            WHERE c.contype='f' AND n.nspname='community' AND rn.nspname<>'community'
            """));
        Assert.Equal(false, await fixture.Scalar("SELECT has_table_privilege('scdc_community','community.schema_migrations','INSERT,UPDATE,DELETE')"));
        Assert.Equal(false, await fixture.Scalar("SELECT has_table_privilege('scdc_community','community.fence_receipts','UPDATE,DELETE')"));
        Assert.Equal(false, await fixture.Scalar("SELECT has_table_privilege('scdc_community','community.access_fences','DELETE')"));
        Assert.Equal(false, await fixture.Scalar("SELECT has_schema_privilege('scdc_community','community','CREATE')"));
    }

    [Fact]
    public async Task Database_health_rejects_admin_credentials_and_shared_owner_database()
    {
        using var admin = fixture.CreateFactory(fixture.AdminTarget);
        using var adminClient = admin.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await adminClient.GetAsync("/health/database")).StatusCode);
        using var shared = fixture.CreateFactory(Environment.GetEnvironmentVariable("ConnectionStrings__Community")!);
        using var sharedClient = shared.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await sharedClient.GetAsync("/health/database")).StatusCode);
    }

    [Fact]
    public void Domain_Application_and_host_have_no_dependency_on_other_service_implementations()
    {
        foreach (var assembly in new[] { typeof(Server).Assembly, typeof(IServerService).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
                reference.Name is "SCDC.BuildingBlocks" or "SCDC.Community.Infrastructure"
                || reference.Name!.StartsWith("Npgsql", StringComparison.Ordinal)
                || reference.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(CommunityProgram).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "SCDC.Api" or "SCDC.Modules.Messaging" or "SCDC.Identity.Infrastructure" or "SCDC.Identity.Api");
    }

    private static async Task Unavailable(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("COMMUNITY_INTEGRATIONS_NOT_CONFIGURED", body.RootElement.GetProperty("code").GetString());
    }
}

public sealed class CommunityHostFixture : IAsyncLifetime
{
    public WebApplicationFactory<CommunityProgram> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string AdminSource { get; private set; } = "";
    public string AdminTarget { get; private set; } = "";
    private readonly string database = "scdc_community_host_" + Guid.NewGuid().ToString("N") + "_test";
    private readonly string directory = Directory.CreateTempSubdirectory("scdc-community-host-").FullName;
    private readonly string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static string Root => SCDC.Api.Tests.Community.CommunityMigrationTests.RepoRoot;

    public async Task InitializeAsync()
    {
        AdminSource = Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase")
            ?? throw new InvalidOperationException("Test database is required.");
        if (!new NpgsqlConnectionStringBuilder(AdminSource).Database!.EndsWith("_test", StringComparison.Ordinal))
            throw new InvalidOperationException("An isolated test database is required.");
        await Execute(AdminSource, "CREATE DATABASE " + database);
        AdminTarget = new NpgsqlConnectionStringBuilder(AdminSource) { Database = database, Pooling = false }.ConnectionString;
        foreach (var name in new[] { "schema.sql", "baseline.sql", "runtime-grants.sql" })
            await Execute(AdminTarget, await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/community", name)));
        var runtime = Environment.GetEnvironmentVariable("ConnectionStrings__Community")
            ?? throw new InvalidOperationException("Community runtime test role is required.");
        Factory = CreateFactory(new NpgsqlConnectionStringBuilder(runtime) { Database = database, Pooling = false }.ConnectionString);
        Client = Factory.CreateClient();
    }

    public WebApplicationFactory<CommunityProgram> CreateFactory(string connection) =>
        new WebApplicationFactory<CommunityProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                TestJwtKeys.Configuration().Concat(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Community"] = connection,
                    ["ConnectionStrings:Identity"] = "",
                    ["ConnectionStrings:Messaging"] = "",
                    ["Modules:Community:Operations:ActiveKeyId"] = "test",
                    ["Modules:Community:Operations:Keys:test"] = key,
                    ["Modules:Community:KeyRingPath"] = directory,
                    ["Telemetry:Enabled"] = "false"
                })));
        });

    public async Task<object?> Scalar(string sql)
    {
        await using var connection = new NpgsqlConnection(AdminTarget);
        await connection.OpenAsync();
        return await new NpgsqlCommand(sql, connection).ExecuteScalarAsync();
    }
    public async Task<string[]> Rows(string settings, string sql)
    {
        await using var connection = new NpgsqlConnection(settings);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var rows = await command.ExecuteReaderAsync();
        var result = new List<string>();
        while (await rows.ReadAsync()) result.Add(rows.GetString(0));
        return result.ToArray();
    }
    private static async Task Execute(string settings, string sql)
    {
        await using var connection = new NpgsqlConnection(settings);
        await connection.OpenAsync();
        await new NpgsqlCommand(sql, connection) { CommandTimeout = 30 }.ExecuteNonQueryAsync();
    }
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null) await Factory.DisposeAsync();
        if (!string.IsNullOrEmpty(AdminSource)) await Execute(AdminSource, "DROP DATABASE IF EXISTS " + database + " WITH (FORCE)");
        Directory.Delete(directory, true);
    }
}
