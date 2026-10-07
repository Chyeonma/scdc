using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Community.Infrastructure.Paging;

internal sealed class ConfigurationCursorCodec(IDataProtectionProvider provider, TimeProvider clock,
    IOptions<CommunityOptions> settings, IOptions<KeyManagementOptions> keys)
{
    private sealed record Payload(int Version, Guid Actor, Guid Server, int Limit, Guid LastId, DateTimeOffset ExpiresAt);
    private CommunityCursorProtector Protector(string collection) => new(provider, $"Community.{collection}.v1", settings, keys);
    public string Encode(string collection, Guid actor, Guid server, int limit, Guid last) =>
        Protector(collection).Protect(JsonSerializer.Serialize(new Payload(1,actor,server,limit,last,clock.GetUtcNow().AddHours(24))));
    public Guid Decode(string collection, string token, Guid actor, Guid server, int limit)
    {
        var json=Protector(collection).Unprotect(token);
        try
        {
            var value=JsonSerializer.Deserialize<Payload>(json);
            if(value is null || value.Version!=1 || value.Actor!=actor || value.Server!=server || value.Limit!=limit
                || value.LastId==Guid.Empty || value.ExpiresAt<=clock.GetUtcNow())throw new InvalidCursorException();
            return value.LastId;
        }
        catch(JsonException){throw new InvalidCursorException();}
    }
}
