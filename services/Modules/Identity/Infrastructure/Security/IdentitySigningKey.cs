using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SCDC.Shared.Core.Authentication;

namespace SCDC.Modules.Identity.Infrastructure.Security;

internal sealed class IdentitySigningKey
{
    public SigningCredentials Credentials { get; }

    public IdentitySigningKey(IdentityOptions options, JwtTrustOptions trust)
    {
        if (!JwtTrustOptions.IsValidKeyId(options.SigningKeyId))
            throw new InvalidOperationException("Identity signing key ID is invalid.");
        var trusted = trust.LoadPublicKeys().SingleOrDefault(key => key.KeyId == options.SigningKeyId)
            ?? throw new InvalidOperationException("The active Identity key must be in the trusted public key set.");
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(options.PrivateKeyPath));
        var publicKey = rsa.ExportParameters(false);
        if (!publicKey.Modulus!.SequenceEqual(trusted.Parameters.Modulus!)
            || !publicKey.Exponent!.SequenceEqual(trusted.Parameters.Exponent!))
            throw new InvalidOperationException("Identity private key does not match its trusted public key.");
        var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = options.SigningKeyId };
        Credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    public static void Initialize(IdentityOptions options, JwtTrustOptions trust)
    {
        if (!JwtTrustOptions.IsValidKeyId(options.SigningKeyId) || string.IsNullOrWhiteSpace(options.PrivateKeyPath)
            || string.IsNullOrWhiteSpace(trust.PublicKeyDirectory))
            throw new InvalidOperationException("Configure private key path, public key directory and signing key ID.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.PrivateKeyPath))!);
        Directory.CreateDirectory(trust.PublicKeyDirectory);
        using var rsa = RSA.Create(3072);
        if (File.Exists(options.PrivateKeyPath))
            rsa.ImportFromPem(File.ReadAllText(options.PrivateKeyPath));
        else
            WriteNew(options.PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem(), privateKey: true);

        var publicPath = Path.Combine(trust.PublicKeyDirectory, options.SigningKeyId + ".pub.pem");
        if (!File.Exists(publicPath)) WriteNew(publicPath, rsa.ExportSubjectPublicKeyInfoPem(), privateKey: false);
        _ = new IdentitySigningKey(options, trust); // Existing keys must match; never silently rotate them.
    }

    private static void WriteNew(string path, string pem, bool privateKey)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = privateKey
            ? UnixFileMode.UserRead | UnixFileMode.UserWrite
            : UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        using var stream = new FileStream(path, options);
        using var writer = new StreamWriter(stream);
        writer.Write(pem);
    }
}
