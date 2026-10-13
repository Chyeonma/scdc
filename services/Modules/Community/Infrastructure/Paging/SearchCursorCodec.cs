using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application.Text;

namespace SCDC.Modules.Community.Infrastructure.Paging;

internal sealed record SearchPosition(int Rank, string Name, Guid Id);

internal sealed class SearchCursorCodec(IDataProtectionProvider provider, TimeProvider clock,
    IOptions<CommunityOptions> settings, IOptions<KeyManagementOptions> keyManagement)
{
    private readonly CommunityCursorProtector _protector = new(provider, "Community.Search.v1", settings, keyManagement);
    private sealed record Payload(int Version, Guid Actor, string Query, int Limit, SearchPosition Position, DateTimeOffset ExpiresAt);
    public string Encode(Guid actor, string queryKey, int limit, SearchPosition last) =>
        _protector.Protect(JsonSerializer.Serialize(new Payload(1, actor, queryKey, limit, last, clock.GetUtcNow().AddHours(24))));
    public SearchPosition Decode(string cursor, Guid actor, string queryKey, int limit)
    {
        var json = _protector.Unprotect(cursor);
        try
        {
            var value = JsonSerializer.Deserialize<Payload>(json);
            if (value is null || value.Version != 1 || value.Actor != actor || value.Query != queryKey || value.Limit != limit
                || value.ExpiresAt <= clock.GetUtcNow() || value.Position is not { Rank: 0 or 1 } position
                || position.Id == Guid.Empty || string.IsNullOrEmpty(position.Name) || !UnicodeTextPolicy.IsValidUnicode(position.Name)
                || (position.Rank == 0) != (position.Name == queryKey))
                throw new InvalidCursorException();
            return position;
        }
        catch (JsonException) { throw new InvalidCursorException(); }
    }
}
