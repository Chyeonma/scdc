using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCDC.Contracts.Identity;

namespace SCDC.BuildingBlocks.Infrastructure.Transport;

// Framework adapters are compiled into each host's own infrastructure, never Shared.Core.
public sealed class RpcTransport(IConfiguration configuration) : IDisposable
{
    private readonly Dictionary<string, GrpcChannel> channels = new(StringComparer.Ordinal);
    private readonly List<X509Certificate2> certificates = [];
    public GrpcChannel Channel(string endpoint, string expectedPeer)
    {
        lock (channels)
        {
            if (channels.TryGetValue(endpoint + expectedPeer, out var cached)) return cached;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var address) || address.Scheme != "https")
                throw new InvalidOperationException("Internal RPC requires an HTTPS endpoint.");
            var ca = X509CertificateLoader.LoadCertificateFromFile(Required(configuration, "CaPath"));
            var own = X509Certificate2.CreateFromPemFile(Required(configuration, "CertificatePath"), Required(configuration, "PrivateKeyPath"));
            certificates.Add(ca); certificates.Add(own);
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(500),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificates = new X509CertificateCollection { own },
                    RemoteCertificateValidationCallback = (_, certificate, _, errors) => certificate is X509Certificate2 peer
                        && (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) == 0
                        && peer.GetNameInfo(X509NameType.DnsName, false) == expectedPeer
                        && Trusted(peer, ca, false)
                }
            };
            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            { HttpHandler = handler, DisposeHttpClient = true, MaxRetryAttempts = 1, MaxReceiveMessageSize = 128 * 1024 });
            channels.Add(endpoint + expectedPeer, channel); return channel;
        }
    }
    public static void ConfigureServer(WebApplicationBuilder builder)
    {
        // TestServer does not bind Kestrel; real hosts always require both TLS peers and files.
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var config = context.Configuration;
            if (!config.GetValue("InternalRpc:Enabled", true)) return;
            var allowed = config.GetSection("InternalRpc:AllowedPeers").Get<string[]>() ?? [];
            if (allowed.Length == 0) throw new InvalidOperationException("Internal RPC requires an explicit peer allowlist.");
            var ca = X509CertificateLoader.LoadCertificateFromFile(Required(config, "CaPath"));
            var own = X509Certificate2.CreateFromPemFile(Required(config, "CertificatePath"), Required(config, "PrivateKeyPath"));
            options.ListenAnyIP(config.GetValue("InternalRpc:HttpPort", 8080), endpoint => endpoint.Protocols = HttpProtocols.Http1);
            options.ListenAnyIP(config.GetValue("InternalRpc:Port", 8443), endpoint =>
            {
                endpoint.Protocols = HttpProtocols.Http2;
                endpoint.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = own, ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateValidation = (peer, _, _) => allowed.Contains(peer.GetNameInfo(X509NameType.DnsName, false), StringComparer.Ordinal)
                        && Trusted(peer, ca, true)
                });
            });
        });
    }
    internal static bool Trusted(X509Certificate2 peer, X509Certificate2 ca, bool client)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(ca);
        // Local demo CA has no CRL endpoint; peer allowlists are separately enforced.
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.ApplicationPolicy.Add(new(client ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1"));
        return chain.Build(peer);
    }
    internal static string Required(IConfiguration configuration, string name) =>
        configuration["InternalRpc:" + name] is { Length: > 0 } value ? value
            : throw new InvalidOperationException("Internal RPC configuration is missing: " + name);
    public void Dispose()
    {
        foreach (var channel in channels.Values) channel.Dispose();
        foreach (var certificate in certificates) certificate.Dispose();
    }
}

public sealed class RpcPeerGuard(IConfiguration configuration, ILogger<RpcPeerGuard> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest,TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest,TResponse> continuation)
    {
        var http = context.GetHttpContext();
        var peer = http.Connection.ClientCertificate;
        var allowed = configuration.GetSection("InternalRpc:AllowedPeers").Get<string[]>() ?? [];
        if (!http.Request.IsHttps || http.Request.Protocol != "HTTP/2" || peer is null
            || !allowed.Contains(peer.GetNameInfo(X509NameType.DnsName, false), StringComparer.Ordinal))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Service peer is not authorized."));
        try { return await continuation(request, context); }
        catch (RpcException) { throw; }
        catch (AccessCheckException error)
        {
            var code = PublicCode(error.Code);
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Access could not be confirmed."),
                new Metadata { { "scdc-error-code", code } });
        }
        catch (Exception error) when (error is FormatException or ArgumentException or OverflowException)
        { throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request.")); }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        { throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled.")); }
        catch (Exception error)
        {
            logger.LogWarning("RPC deferred {FailureType}", error.GetType().Name);
            throw new RpcException(new Status(StatusCode.Unavailable, "Service is unavailable."));
        }
    }
    public static string PublicCode(string? code) => code is "ACCESS_CHECK_UNAVAILABLE" or "ACCESS_PENDING"
        or "INVALID_PERMIT_REQUEST" or "ACCOUNT_ACCESS_DENIED" or "RESOURCE_NOT_FOUND"
        or "ACCESS_CHANGED" or "Common.Unauthorized" ? code : "ACCESS_CHECK_UNAVAILABLE";
}
