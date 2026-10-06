using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Community.Infrastructure.Paging;

internal sealed class ServerCursorCodec(IDataProtectionProvider provider, TimeProvider clock,
    IOptions<CommunityOptions> settings, IOptions<KeyManagementOptions> keyManagement)
{
    private readonly IDataProtector _protector = provider.CreateProtector("Community.MyServers.v1");
    private sealed record Payload(int Version, Guid Actor, int Limit, Guid LastId, DateTimeOffset ExpiresAt);
    public string Encode(Guid actor, int limit, Guid lastId)
    {
        CheckStorage(createDirectory: true);
        try
        {
            return _protector.Protect(JsonSerializer.Serialize(new Payload(1, actor, limit, lastId, clock.GetUtcNow().AddHours(24))));
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new CursorKeyUnavailableException();
        }
    }
    public Guid Decode(string cursor, Guid actor, int limit)
    {
        if (cursor.Length > 4096)
            throw new InvalidCursorException();
        CheckStorage(createDirectory: false);
        string json;
        try
        {
            json = _protector.Unprotect(cursor);
        }
        catch (CryptographicException ex)
        {
            // Missing persisted keys is operational failure, not an invalid request.
            if (ex.InnerException is IOException or UnauthorizedAccessException)
                throw new CursorKeyUnavailableException();
            throw new InvalidCursorException();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new CursorKeyUnavailableException(); }
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
    private void CheckStorage(bool createDirectory)
    {
        try
        {
            var path = settings.Value.KeyRingPath;
            if (File.Exists(path) || (!createDirectory && !Directory.Exists(path)))
                throw new CursorKeyUnavailableException();
            if (createDirectory)
                Directory.CreateDirectory(path);
            _ = keyManagement.Value.XmlRepository?.GetAllElements();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            throw new CursorKeyUnavailableException();
        }
    }
}
internal sealed class InvalidCursorException : Exception;
internal sealed class CursorKeyUnavailableException : Exception;
