extern alias IdentityApi;
extern alias CommunityApi;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;
using IdentityProgram = IdentityApi::IdentityProgram;
using CommunityProgram = CommunityApi::CommunityProgram;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class IdentityNetworkTests(IdentityNetworkFixture fixture) : IClassFixture<IdentityNetworkFixture>
{
    [Fact]
    public void Hosts_and_identity_layers_have_no_cross_implementation_references()
    {
        Assert.DoesNotContain(typeof(ApiProgram).Assembly.GetReferencedAssemblies(), name => name.Name is "SCDC.Identity.Infrastructure" or "SCDC.Identity.Application" or "SCDC.Identity.Domain");
        Assert.DoesNotContain(typeof(SCDC.Modules.Identity.Domain.User).Assembly.GetReferencedAssemblies(), name => name.Name!.StartsWith("Microsoft.AspNetCore") || name.Name.StartsWith("Npgsql") || name.Name.StartsWith("Grpc"));
        Assert.DoesNotContain(typeof(SCDC.Modules.Identity.Application.IAuthenticationService).Assembly.GetReferencedAssemblies(), name => name.Name!.StartsWith("Microsoft.AspNetCore") || name.Name.StartsWith("Npgsql") || name.Name.StartsWith("Grpc"));
        Assert.Null(fixture.Monolith.Services.GetService<SCDC.Modules.Identity.Infrastructure.Persistence.IdentityDbContext>());
        Assert.True(string.IsNullOrEmpty(fixture.Monolith.Services.GetRequiredService<IConfiguration>().GetConnectionString("Identity")));
    }

    [Fact]
    public void Permit_binding_survives_serialization_and_rejects_changed_binding()
    {
        var now=DateTimeOffset.UtcNow;
        var request=new PermitRequest(new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),now.AddMinutes(5)),IdentityPurpose.DmRead,Guid.NewGuid(),Guid.NewGuid());
        var permit=new IdentityPermit(Guid.NewGuid(),AccessOwner.Messaging,request,[new(1,request.Actor.UserId,Guid.Empty,1)],now,now.AddSeconds(5));
        var wire=IdentityWire.Write(permit).Clone();
        Assert.NotSame(wire.Actor,wire.Request.Actor);
        Assert.Equal(request,IdentityWire.Read(wire).Request);
        wire.Request.Actor.UserId=Guid.NewGuid().ToString();
        Assert.Throws<FormatException>(()=>IdentityWire.Read(wire));
    }

    [Fact]
    public async Task Separate_hosts_permit_profile_lookup_dm_and_both_fence_callbacks_work_over_mtls()
    {
        var a=await fixture.Actor();var b=await fixture.Actor();
        using var scope=fixture.Monolith.Services.CreateScope();
        var directory=scope.ServiceProvider.GetRequiredService<IUserDirectory>();
        Assert.Equal(a.UserId,(await directory.FindByUsernameAsync(a.Username,default))!.Id);
        Assert.Equal(b.UserId,(await directory.FindByIdAsync(b.UserId,default))!.Id);
        var summaries=await scope.ServiceProvider.GetRequiredService<IHistoricalUserSummaryReader>().ReadAsync([a.UserId,b.UserId,Guid.NewGuid()],default);
        Assert.Equal(3,summaries.Count);Assert.Contains(summaries.Values,item=>item.Availability=="unavailable");
        var server=await fixture.Post(fixture.Community,"/api/v1/servers",new{clientOperationId=Guid.NewGuid(),name="Remote Identity",visibility="public"},a.Token,201);
        Assert.Equal(a.UserId.ToString(),server!["ownerUserId"]!.GetValue<string>());
        var conversation=await fixture.Post(fixture.Chat,"/api/v1/direct-conversations",new{peerUserId=b.UserId},a.Token,200);
        var space=conversation!["id"]!.GetValue<string>();
        var events=new List<JsonNode>();var closed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var hub=new HubConnectionBuilder().WithUrl(fixture.Chat.BaseAddress+"hubs/chat",options=>options.AccessTokenProvider=()=>Task.FromResult<string?>(b.Token)).Build();
        hub.On<JsonNode>("MessageChanged",node=>{lock(events)events.Add(node);});hub.Closed+=_=>{closed.TrySetResult();return Task.CompletedTask;};
        await hub.StartAsync();await hub.InvokeAsync("SubscribeConversation",new{conversationId=space});
        var operation=Guid.NewGuid();
        var first=await fixture.Post(fixture.Chat,$"/api/v1/direct-conversations/{space}/messages",new{clientMessageId=operation,content="Real gRPC and mTLS"},a.Token,200);
        var replay=await fixture.Post(fixture.Chat,$"/api/v1/direct-conversations/{space}/messages",new{clientMessageId=operation,content="Real gRPC and mTLS"},a.Token,200);
        Assert.Equal(first!["id"]!.GetValue<string>(),replay!["id"]!.GetValue<string>());
        await IdentityNetworkFixture.Until(()=>{lock(events)return events.Count==1;});
        var cutoff=Stopwatch.StartNew();
        await fixture.Post(fixture.Identity,"/api/v1/auth/logout",new{refreshToken=b.Refresh},null,204);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(cutoff.Elapsed<TimeSpan.FromSeconds(5));
        using var denied=new HttpRequestMessage(HttpMethod.Get,"/api/v1/users/me");denied.Headers.Authorization=new("Bearer",b.Token);
        Assert.Equal(HttpStatusCode.Unauthorized,(await fixture.Identity.SendAsync(denied)).StatusCode);
        await fixture.Post(fixture.Chat,$"/api/v1/direct-conversations/{space}/messages",new{clientMessageId=Guid.NewGuid(),content="After peer revocation"},a.Token,200);
        await Task.Delay(300);lock(events)Assert.Single(events);
    }

    [Theory]
    [InlineData("untrusted")]
    [InlineData("other-service")]
    [InlineData("expired")]
    public async Task Rpc_rejects_untrusted_wrong_peer_and_expired_client_certificates(string peer)
    {
        var settings=fixture.ClientSettings(peer);
        using var transport=new RpcTransport(settings);
        var client=new W.IdentityDirectory.IdentityDirectoryClient(transport.Channel(fixture.IdentityRpc,"identity-service"));
        await Assert.ThrowsAsync<RpcException>(async()=>await client.LookupProfilesAsync(new W.ProfileQuery(),deadline:DateTime.UtcNow.AddSeconds(2)));
    }

    [Fact]
    public async Task Rpc_rejects_a_trusted_server_with_the_wrong_service_identity()
    {
        using var transport=new RpcTransport(fixture.ClientSettings("chat-service"));
        var client=new W.IdentityDirectory.IdentityDirectoryClient(transport.Channel(fixture.MonolithRpc,"identity-service"));
        await Assert.ThrowsAsync<RpcException>(async()=>await client.LookupProfilesAsync(new W.ProfileQuery(),deadline:DateTime.UtcNow.AddSeconds(2)));
    }

    [Fact]
    public async Task User_bearer_token_cannot_replace_a_client_certificate()
    {
        var user=await fixture.Actor();
        // Trusting this test server deliberately does not provide a client identity.
        using var handler=new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator};
        using var channel=Grpc.Net.Client.GrpcChannel.ForAddress(fixture.IdentityRpc,new Grpc.Net.Client.GrpcChannelOptions{HttpHandler=handler});
        var client=new W.IdentityDirectory.IdentityDirectoryClient(channel);
        await Assert.ThrowsAsync<RpcException>(async()=>await client.LookupProfilesAsync(new W.ProfileQuery(),new Metadata{{"authorization","Bearer "+user.Token}},deadline:DateTime.UtcNow.AddSeconds(2)));
    }

    [Fact]
    public async Task Authenticated_peer_cannot_select_an_owner_outside_its_rpc_permissions()
    {
        var configuration=fixture.IdentityHost.Services.GetRequiredService<IConfiguration>();
        var before=configuration["InternalRpc:PeerOwners:chat-service:0"];
        configuration["InternalRpc:PeerOwners:chat-service:0"]="";
        try
        {
            using var transport=new RpcTransport(fixture.ClientSettings("chat-service"));
            var client=new W.IdentityAccess.IdentityAccessClient(transport.Channel(fixture.IdentityRpc,"identity-service"));
            var error=await Assert.ThrowsAsync<RpcException>(async()=>await client.AcquirePermitAsync(new W.AccessRequest{Owner=W.Owner.Messaging},deadline:DateTime.UtcNow.AddSeconds(2)));
            Assert.Equal(StatusCode.PermissionDenied,error.StatusCode);
        }
        finally{configuration["InternalRpc:PeerOwners:chat-service:0"]=before;}
    }

    [Fact]
    public async Task Public_routes_belong_only_to_their_own_host()
    {
        Assert.Equal(HttpStatusCode.NotFound,(await fixture.Chat.PostAsJsonAsync("/api/v1/auth/login",new{login="missing",password="missing"})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await fixture.Identity.GetAsync("/hubs/chat")).StatusCode);
    }

    [Fact]
    public async Task Remote_deadline_and_caller_cancellation_do_not_retry_or_return_profiles()
    {
        using var scope = fixture.Monolith.Services.CreateScope();
        var directory = scope.ServiceProvider.GetRequiredService<IUserDirectory>();
        fixture.IdentityFaults.DelayNext = 1;
        var elapsed = Stopwatch.StartNew();
        var error = await Assert.ThrowsAsync<AccessCheckException>(() => directory.FindByIdAsync(Guid.NewGuid(), default));
        Assert.Equal("ACCESS_CHECK_UNAVAILABLE", error.Code);
        Assert.InRange(elapsed.Elapsed.TotalSeconds, 1, 4);
        Assert.Equal(1, fixture.IdentityFaults.Delayed);
        fixture.IdentityFaults.DelayNext = 1;
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        elapsed.Restart();
        var failure = await Record.ExceptionAsync(() => directory.FindByIdAsync(Guid.NewGuid(), cancelled.Token));
        Assert.True(failure is OperationCanceledException or AccessCheckException);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(1));
        Assert.Equal(2, fixture.IdentityFaults.Delayed);
    }

    [Fact]
    public async Task Lost_fence_ack_after_owner_commit_is_replayed_from_durable_journal()
    {
        var actor = await fixture.Actor();
        fixture.OwnerFaults.DropNextAck = 1;
        using var response = await fixture.Identity.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = actor.Refresh });
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NoContent, HttpStatusCode.Accepted });
        using var scope = fixture.IdentityHost.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IRevocationStatusReader>();
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonNode>();
            var operation = Guid.Parse(body!["operationId"]!.GetValue<string>());
            var ready = false;
            for (var i = 0; i < 160 && !ready; i++)
            {
                ready = (await reader.ReadAsync(operation, actor.UserId, default))?.Phase == "committed";
                if (!ready) await Task.Delay(100);
            }
            Assert.True(ready, "Worker must finish the persisted operation after its ACK was lost.");
        }
        Assert.Equal(1, fixture.OwnerFaults.Dropped);
        Assert.True(fixture.OwnerFaults.FenceCalls >= 2);
        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        denied.Headers.Authorization = new("Bearer", actor.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Identity.SendAsync(denied)).StatusCode);
    }
}

// Test-only transport faults: the handler commits before a response is lost.
public sealed class NetworkFaults : Interceptor
{
    public int DelayNext, DropNextAck, Delayed, Dropped, FenceCalls;
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.EndsWith("/LookupProfiles", StringComparison.Ordinal)
            && Interlocked.Exchange(ref DelayNext, 0) == 1)
        {
            Interlocked.Increment(ref Delayed);
            await Task.Delay(TimeSpan.FromSeconds(10), context.CancellationToken);
        }
        var response = await continuation(request, context);
        if (context.Method.Contains("MessagingFence/Apply", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref FenceCalls);
            if (Interlocked.Exchange(ref DropNextAck, 0) == 1)
            {
                Interlocked.Increment(ref Dropped);
                throw new RpcException(new Status(StatusCode.Unavailable, "Injected lost ACK after commit."));
            }
        }
        return response;
    }
}

public sealed class IdentityNetworkFixture : IAsyncLifetime
{
    internal SCDC.Modules.Identity.Infrastructure.Email.IAccountEmailSender? EmailSender { get; init; }
    public WebApplicationFactory<ApiProgram> Monolith { get; private set; }=null!;
    public WebApplicationFactory<IdentityProgram> IdentityHost { get; private set; }=null!;
    public WebApplicationFactory<CommunityProgram> CommunityHost { get; private set; }=null!;
    public HttpClient Community { get; private set; }=null!;
    public string CommunityRpc { get; private set; }="";
    public HttpClient Chat { get; private set; }=null!;
    public HttpClient Identity { get; private set; }=null!;
    public string IdentityRpc { get; private set; }="";
    public string MonolithRpc { get; private set; }="";
    public NetworkFaults IdentityFaults { get; } = new();
    public NetworkFaults OwnerFaults { get; } = new();
    private readonly string directory=Directory.CreateTempSubdirectory("scdc-rpc-test-").FullName;
    private readonly string database="scdc_identity_net_"+Guid.NewGuid().ToString("N")[..12];
    private readonly Dictionary<string,string> databases = new();
    private string admin="";
    private const string Password="NetworkIdentity!2026";
    public IConfiguration ClientSettings(string peer)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
    { ["InternalRpc:CaPath"]=Path.Combine(directory,"ca.crt"),["InternalRpc:CertificatePath"]=Path.Combine(directory,peer+".crt"),["InternalRpc:PrivateKeyPath"]=Path.Combine(directory,peer+".key") }).Build();
    public async Task InitializeAsync()
    {
        Certificates();
        admin=Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase") ?? throw new InvalidOperationException("Test database is required.");
        if(!new NpgsqlConnectionStringBuilder(admin).Database!.EndsWith("_test",StringComparison.Ordinal))throw new InvalidOperationException("An isolated test database is required.");
        var root = SCDC.Api.Tests.Community.CommunityMigrationTests.RepoRoot;
        foreach (var owner in new[] { "Identity", "Community", "Messaging" })
        {
            var name = database + "_" + owner.ToLowerInvariant() + "_test";
            await using (var connection = new NpgsqlConnection(admin))
            {
                await connection.OpenAsync();
                await new NpgsqlCommand("CREATE DATABASE " + name, connection).ExecuteNonQueryAsync();
            }
            var target = new NpgsqlConnectionStringBuilder(admin) { Database = name, Pooling = false }.ConnectionString;
            databases[owner] = target;
            await using var destination = new NpgsqlConnection(target);
            await destination.OpenAsync();
            async Task Execute(string sql) => await new NpgsqlCommand(sql, destination) { CommandTimeout = 30 }.ExecuteNonQueryAsync();
            if (owner == "Community")
            {
                foreach (var file in new[] { "schema.sql", "baseline.sql", "runtime-grants.sql" })
                    await Execute(await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/community", file)));
            }
            else if (owner == "Identity")
            {
                await Execute("CREATE EXTENSION pg_trgm;");
                await Execute(await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/identity/schema.sql")));
                var baseline = await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/identity/baseline.sql"));
                await Execute(baseline.Replace(":'baseline_checksum'", "'" + new string('0', 64) + "'", StringComparison.Ordinal));
                await Execute(await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/identity/runtime-grants.sql")));
                await IdentityEmailTestDatabase.UpgradeAsync(target);
            }
            else
            {
                var schema = await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/schema.sql"));
                await Execute(string.Join('\n', schema.Split('\n').Where(line => !line.TrimStart().StartsWith('\\'))));
                var roles = await File.ReadAllTextAsync(Path.Combine(root, "database/postgres/module-roles.sql"));
                await Execute("BEGIN;\n" + roles[roles.IndexOf("REVOKE CREATE ON SCHEMA", StringComparison.Ordinal)..]);
                await Execute("DROP SCHEMA identity CASCADE; DROP SCHEMA community CASCADE;");
            }
        }
        var prefix="scdc:auth:network:"+Guid.NewGuid().ToString("N")+":";
        Dictionary<string,string?> Settings(string peer,string allowed)
        {
            var values=TestJwtKeys.Configuration();
            foreach(var pair in ClientSettings(peer).AsEnumerable())values[pair.Key]=pair.Value;
            values["InternalRpc:AllowedPeers:0"]=allowed;values["InternalRpc:Port"]="0";values["InternalRpc:HttpPort"]="0";
            values["InternalRpc:PeerOwners:chat-service:0"]="Messaging";
            values["InternalRpc:PeerOwners:community-service:0"]="Community";
            values["InternalRpc:PermitDelegations:community-service:Messaging:0"]="ChannelRead";
            values["InternalRpc:Community:Peer"]="community-service";
            values["Authentication:Sessions:KeyPrefix"]=prefix;
            values["Modules:Messaging:Dispatcher:Enabled"]="true";
            var currentOwner = peer == "identity-service" ? "Identity" : peer == "community-service" ? "Community" : "Messaging";
            foreach (var owner in new[] { "Identity", "Community", "Messaging" })
            {
                values["ConnectionStrings:" + owner] = "";
                if (owner != currentOwner) continue;
                var existing = Environment.GetEnvironmentVariable("ConnectionStrings__" + owner) ?? throw new InvalidOperationException("Runtime test role is required.");
                values["ConnectionStrings:" + owner] = new NpgsqlConnectionStringBuilder(existing)
                { Database = new NpgsqlConnectionStringBuilder(databases[owner]).Database, Pooling = false }.ConnectionString;
            }
            values["Modules:Community:Operations:ActiveKeyId"]="network";
            values["Modules:Community:Operations:Keys:network"]=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            values["Modules:Community:KeyRingPath"]=Path.Combine(directory,"community-keys");
            values["Modules:Identity:UserSearch:CursorKeyRingPath"]=Path.Combine(directory,"cursors");
            values["Modules:Identity:ExposeDevelopmentTokens"]="true";
            values["Modules:Identity:Email:KeyRingPath"]=Path.Combine(directory,"email-keys");
            values["InternalRpc:Identity:Endpoint"]="https://127.0.0.1:1";
            return values;
        }
        var mono=Settings("chat-service","identity-service");
        mono["InternalRpc:AllowedPeers:1"]="community-service";
        mono["ConnectionStrings:Identity"]="";mono["Modules:Identity:PrivateKeyPath"]="";
        Monolith=new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder=>
        { builder.UseEnvironment("Development");builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(mono));
          builder.ConfigureServices(services => { services.AddSingleton(OwnerFaults); services.Configure<GrpcServiceOptions>(options => options.Interceptors.Add<NetworkFaults>()); }); });
        Monolith.UseKestrel(0);Monolith.StartServer();
        MonolithRpc=Address(Monolith.Services,"https");Chat=new HttpClient{BaseAddress=new Uri(Address(Monolith.Services,"http"))};
        var community = Settings("community-service", "identity-service");
        community["CommunityHost:Integrated"] = "true";
        community["InternalRpc:AllowedPeers:1"] = "chat-service";
        community["InternalRpc:Messaging:Endpoint"] = MonolithRpc;
        CommunityHost = new WebApplicationFactory<CommunityProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("CommunityHost:Integrated", "true");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(community));
        });
        CommunityHost.UseKestrel(0); CommunityHost.StartServer();
        CommunityRpc = Address(CommunityHost.Services, "https");
        Community = new HttpClient { BaseAddress = new Uri(Address(CommunityHost.Services, "http")) };
        var identity=Settings("identity-service","chat-service");
        identity["InternalRpc:AllowedPeers:1"]="community-service";
        if (EmailSender is not null)
        {
            identity["Modules:Identity:Email:Enabled"] = "true";
            identity["Modules:Identity:Email:SenderAddress"] = "scdc@example.test";
            var smtpSecret = Path.Combine(directory, "smtp-test.password");
            await File.WriteAllTextAsync(smtpSecret, "test-only");
            identity["Modules:Identity:Email:AppPasswordFile"] = smtpSecret;
            identity["Modules:Identity:Email:PollSeconds"] = "1";
            identity["Modules:Identity:ExposeDevelopmentTokens"] = "false";
        }
        var identityDatabase = new NpgsqlConnectionStringBuilder(identity["ConnectionStrings:Identity"]!);
        var databasePasswordFile = Path.Combine(directory,"database.password");
        await File.WriteAllTextAsync(databasePasswordFile,identityDatabase.Password);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(databasePasswordFile,UnixFileMode.UserRead|UnixFileMode.UserWrite);
        identityDatabase.Password="";
        identity["ConnectionStrings:Identity"]=identityDatabase.ConnectionString;
        identity["ConnectionStrings:IdentityPasswordFile"]=databasePasswordFile;
        identity["InternalRpc:Community:Endpoint"]=CommunityRpc;identity["InternalRpc:Messaging:Endpoint"]=MonolithRpc;
        IdentityHost=new WebApplicationFactory<IdentityProgram>().WithWebHostBuilder(builder=>
        { builder.UseEnvironment("Development");builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(identity));
          builder.ConfigureServices(services =>
          {
              services.AddSingleton(IdentityFaults);
              services.Configure<GrpcServiceOptions>(options => options.Interceptors.Add<NetworkFaults>());
              if (EmailSender is not null) services.AddSingleton(EmailSender);
          }); });
        IdentityHost.UseKestrel(0);IdentityHost.StartServer();
        IdentityRpc=Address(IdentityHost.Services,"https");Identity=new HttpClient{BaseAddress=new Uri(Address(IdentityHost.Services,"http"))};
        Monolith.Services.GetRequiredService<IConfiguration>()["InternalRpc:Identity:Endpoint"]=IdentityRpc;
        CommunityHost.Services.GetRequiredService<IConfiguration>()["InternalRpc:Identity:Endpoint"]=IdentityRpc;
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Identity,"/api/v1/health/identity/access");
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Chat);
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Community,"/health/ready");
    }
    private static string Address(IServiceProvider services,string scheme)=>services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses
        .First(value=>value.StartsWith(scheme+":",StringComparison.Ordinal)).Replace("[::]","127.0.0.1").TrimEnd('/')+"/";
    public sealed record ActorInfo(Guid UserId,string Username,string Token,string Refresh);
    public async Task<ActorInfo> Actor()
    {
        var username="network_"+Guid.NewGuid().ToString("N")[..16];
        var created=await Post(Identity,"/api/v1/auth/register",new{username,displayName=username,email=username+"@example.test",password=Password},null,201);
        await Post(Identity,"/api/v1/auth/verify-email",new{token=created!["developmentVerificationToken"]!.GetValue<string>()},null,204);
        var login=await Post(Identity,"/api/v1/auth/login",new{login=username,password=Password},null,200);
        return new(Guid.Parse(login!["user"]!["id"]!.GetValue<string>()),username,login["accessToken"]!.GetValue<string>(),login["refreshToken"]!.GetValue<string>());
    }
    public async Task<JsonNode?> Post(HttpClient client,string path,object body,string? token,int expected)
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};
        if(token is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        using var response=await client.SendAsync(request);Assert.Equal(expected,(int)response.StatusCode);
        var text=await response.Content.ReadAsStringAsync();return text.Length==0?null:JsonNode.Parse(text);
    }
    public static async Task Until(Func<bool> test)
    { var clock=Stopwatch.StartNew();while(!test()){Assert.True(clock.Elapsed<TimeSpan.FromSeconds(10));await Task.Delay(30);} }
    private void Certificates()
    {
        using var rootKey=RSA.Create(2048);
        var caRequest=new CertificateRequest("CN=SCDC network tests",rootKey,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true,false,0,true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign|X509KeyUsageFlags.CrlSign,true));
        using var ca=caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-7),DateTimeOffset.UtcNow.AddDays(7));
        File.WriteAllText(Path.Combine(directory,"ca.crt"),ca.ExportCertificatePem());
        foreach(var peer in new[]{"identity-service","chat-service","community-service","other-service","expired","untrusted"})
        {
            using var key=RSA.Create(2048);
            var name=peer=="expired"?"chat-service":peer;
            var request=new CertificateRequest("CN="+name,key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
            var san=new SubjectAlternativeNameBuilder();san.AddDnsName(name);san.AddDnsName("localhost");san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection{new("1.3.6.1.5.5.7.3.1"),new("1.3.6.1.5.5.7.3.2")},true));
            using var issued=peer=="untrusted"?request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddDays(1))
                :request.Create(ca,DateTimeOffset.UtcNow.AddDays(-3),DateTimeOffset.UtcNow.AddDays(peer=="expired"?-1:1),RandomNumberGenerator.GetBytes(16));
            File.WriteAllText(Path.Combine(directory,peer+".crt"),issued.ExportCertificatePem());
            File.WriteAllText(Path.Combine(directory,peer+".key"),key.ExportPkcs8PrivateKeyPem());
            if(!OperatingSystem.IsWindows())File.SetUnixFileMode(Path.Combine(directory,peer+".key"),UnixFileMode.UserRead|UnixFileMode.UserWrite);
        }
    }
    public async Task DisposeAsync()
    {
        Chat?.Dispose();Identity?.Dispose();Community?.Dispose();if(CommunityHost is not null)await CommunityHost.DisposeAsync();if(IdentityHost is not null)await IdentityHost.DisposeAsync();if(Monolith is not null)await Monolith.DisposeAsync();
        if(admin.Length>0){await using var connection=new NpgsqlConnection(admin);await connection.OpenAsync();foreach(var target in databases.Values){var name=new NpgsqlConnectionStringBuilder(target).Database;await new NpgsqlCommand("DROP DATABASE IF EXISTS "+name+" WITH (FORCE)",connection).ExecuteNonQueryAsync();}}
        Directory.Delete(directory,true);
    }
}
