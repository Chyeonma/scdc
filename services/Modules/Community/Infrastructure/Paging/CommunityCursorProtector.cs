using System.Security.Cryptography;
using System.Xml;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Community.Infrastructure.Paging;

internal sealed class CommunityCursorProtector(IDataProtectionProvider provider, string purpose,
    IOptions<CommunityOptions> settings, IOptions<KeyManagementOptions> keyManagement)
{
    private readonly IDataProtector _protector = provider.CreateProtector(purpose);
    public string Protect(string json)
    {
        CheckStorage(createDirectory: true);
        try { return _protector.Protect(json); }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        { throw new CursorKeyUnavailableException(); }
    }
    public string Unprotect(string cursor)
    {
        if (cursor.Length > 4096) throw new InvalidCursorException();
        CheckStorage(createDirectory: false);
        try { return _protector.Unprotect(cursor); }
        catch (CryptographicException ex)
        {
            if (ex.InnerException is IOException or UnauthorizedAccessException) throw new CursorKeyUnavailableException();
            throw new InvalidCursorException();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new CursorKeyUnavailableException(); }
    }
    private void CheckStorage(bool createDirectory)
    {
        try
        {
            var path = settings.Value.KeyRingPath;
            if (File.Exists(path) || (!createDirectory && !Directory.Exists(path))) throw new CursorKeyUnavailableException();
            if (createDirectory) Directory.CreateDirectory(path);
            _ = keyManagement.Value.XmlRepository?.GetAllElements();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        { throw new CursorKeyUnavailableException(); }
    }
}
