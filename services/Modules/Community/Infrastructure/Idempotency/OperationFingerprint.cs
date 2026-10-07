using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using SCDC.Modules.Community.Features.Permissions.Domain;

namespace SCDC.Modules.Community.Infrastructure.Idempotency;

internal static class OperationFingerprint
{
    public static byte[] ComputeRole(byte[] key, Guid actor, Guid server, Guid operation, string name,
        IReadOnlyList<string> permissions, short version = 1)
    {
        if (version != 1) throw new FingerprintKeyUnavailableException();
        using var body = new MemoryStream();
        WriteString(body, name);
        body.WriteByte(ManagementPermissions.Mask(permissions));
        using var input = new MemoryStream();
        input.Write(Encoding.ASCII.GetBytes("SCDC.Community.Write.v1\0create_role\0"));
        WriteGuid(input, actor);
        WriteGuid(input, server);
        WriteGuid(input, operation);
        WriteInt(input, checked((int)body.Length));
        input.Write(body.ToArray());
        return HMACSHA256.HashData(key, input.ToArray());
    }
    public static byte[] Compute(byte[] key, Guid actor, Guid operation, string name, string? description, short visibility, short version = 1)
    {
        if (version != 1)
            throw new FingerprintKeyUnavailableException();
        using var body = new MemoryStream();
        WriteString(body, name);
        body.WriteByte(description is null ? (byte)0 : (byte)1);
        if (description is not null)
            WriteString(body, description);
        body.WriteByte((byte)visibility);
        using var input = new MemoryStream();
        input.Write(Encoding.ASCII.GetBytes("SCDC.Community.Write.v1\0create_server\0"));
        WriteGuid(input, actor);
        WriteGuid(input, Guid.Empty);
        WriteGuid(input, operation);
        WriteInt(input, checked((int)body.Length));
        input.Write(body.ToArray());
        return HMACSHA256.HashData(key, input.ToArray());
    }
    private static void WriteGuid(Stream stream, Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes, bigEndian: true, out _);
        stream.Write(bytes);
    }
    private static void WriteInt(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }
    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt(stream, bytes.Length);
        stream.Write(bytes);
    }
}
