using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SCDC.Modules.Messaging.Infrastructure.Security;

public sealed class MessageFingerprint(IConfiguration configuration)
{
    public string ActiveKeyId => configuration["Modules:Messaging:ActiveFingerprintKeyId"] ?? "dm-local-v1";
    public byte[]? Compute(string keyId, Guid space, Guid author, Guid client, string content)
    {
        var directory = configuration["Modules:Messaging:FingerprintKeyDirectory"];
        if (string.IsNullOrEmpty(directory) || keyId.Length is < 1 or > 64
            || keyId.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) return null;
        try
        {
            var key = File.ReadAllBytes(Path.Combine(directory, keyId + ".key"));
            try { return key.Length < 32 ? null : Hash(key, space, author, client, content); }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return null; }
    }
    public static byte[] Hash(byte[] key, Guid space, Guid author, Guid client, string content)
    {
        var body = Encoding.UTF8.GetBytes(content);
        var domain = Encoding.ASCII.GetBytes("SCDC.Send.v1\0");
        var input = new byte[domain.Length + 48 + 4 + body.Length];
        domain.CopyTo(input, 0);
        space.ToByteArray(bigEndian: true).CopyTo(input, domain.Length);
        author.ToByteArray(bigEndian: true).CopyTo(input, domain.Length + 16);
        client.ToByteArray(bigEndian: true).CopyTo(input, domain.Length + 32);
        BinaryPrimitives.WriteUInt32BigEndian(input.AsSpan(domain.Length + 48), (uint)body.Length);
        body.CopyTo(input, domain.Length + 52);
        try { return HMACSHA256.HashData(key, input); }
        finally { CryptographicOperations.ZeroMemory(input); CryptographicOperations.ZeroMemory(body); }
    }
}
