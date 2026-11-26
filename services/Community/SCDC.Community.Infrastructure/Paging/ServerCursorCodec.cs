using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Community.Infrastructure.Paging;

internal sealed class ServerCursorCodec(IDataProtectionProvider provider, TimeProvider clock,
    IOptions<CommunityOptions> settings, IOptions<KeyManagementOptions> keyManagement)
{
    private readonly CommunityCursorProtector _protector = new(provider, "Community.MyServers.v1", settings, keyManagement);
    private sealed record Payload(int Version, Guid Actor, int Limit, Guid LastId, DateTimeOffset ExpiresAt);
    public string Encode(Guid actor, int limit, Guid lastId)
    {
        return _protector.Protect(JsonSerializer.Serialize(new Payload(1, actor, limit, lastId, clock.GetUtcNow().AddHours(24))));
    }
    public Guid Decode(string cursor, Guid actor, int limit)
    {
        var json = _protector.Unprotect(cursor);
        try
        {
            var value = JsonSerializer.Deserialize<Payload>(json);
            if (value is null || value.Version != 1 || value.Actor != actor || value.Limit != limit || value.LastId == Guid.Empty
                || value.ExpiresAt <= clock.GetUtcNow())
                throw new InvalidCursorException();
            return value.LastId;
        }
        catch (JsonException) { throw new InvalidCursorException(); }
    }
}
internal sealed class InvalidCursorException : Exception;
internal sealed class CursorKeyUnavailableException : Exception;
