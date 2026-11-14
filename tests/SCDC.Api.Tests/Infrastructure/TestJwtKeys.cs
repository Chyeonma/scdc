using System.Security.Cryptography;

namespace SCDC.Api.Tests.Infrastructure;

internal static class TestJwtKeys
{
    internal const string KeyId = "test-identity-v1";
    internal static readonly string DirectoryPath = Directory.CreateTempSubdirectory("scdc-jwt-tests-").FullName;
    internal static readonly string PrivateKeyPath = Path.Combine(DirectoryPath, "private.pem");
    internal static readonly string PublicKeyDirectory = Path.Combine(DirectoryPath, "public");

    static TestJwtKeys()
    {
        using var rsa = RSA.Create(2048);
        Directory.CreateDirectory(PublicKeyDirectory);
        File.WriteAllText(PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(PrivateKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.WriteAllText(Path.Combine(PublicKeyDirectory, KeyId + ".pub.pem"), rsa.ExportSubjectPublicKeyInfoPem());
    }

    internal static Dictionary<string, string?> Configuration() => new()
    {
        ["Authentication:Jwt:Issuer"] = "SCDC",
        ["Authentication:Jwt:Audience"] = "SCDC.WebClient",
        ["Authentication:Jwt:PublicKeyDirectory"] = PublicKeyDirectory,
        ["Authentication:Jwt:KeyIds:0"] = KeyId,
        ["Modules:Identity:PrivateKeyPath"] = PrivateKeyPath,
        ["Modules:Identity:SigningKeyId"] = KeyId
    };
}
