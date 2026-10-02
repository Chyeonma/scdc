using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class ClamAvFileScanner(IOptions<AttachmentStorageOptions> options) : IFileScanner
{
    public async Task<bool> IsCleanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var client = new TcpClient();
        await client.ConnectAsync(options.Value.ClamAvHost, options.Value.ClamAvPort, timeout.Token);
        await using var stream = client.GetStream();
        await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(size, (uint)content.Length);
        await stream.WriteAsync(size, timeout.Token);
        await stream.WriteAsync(content, timeout.Token);
        await stream.WriteAsync(new byte[4], timeout.Token);
        await stream.FlushAsync(timeout.Token);

        var reply = new byte[512];
        var length = 0;
        while (length < reply.Length)
        {
            var count = await stream.ReadAsync(reply.AsMemory(length), timeout.Token);
            if (count == 0) break;
            length += count;
            if (Array.IndexOf(reply, (byte)0, 0, length) >= 0) break;
        }
        var result = Encoding.UTF8.GetString(reply, 0, length).TrimEnd('\0', '\r', '\n');
        if (result.EndsWith(" OK", StringComparison.Ordinal)) return true;
        if (result.EndsWith(" FOUND", StringComparison.Ordinal)) return false;
        throw new IOException("ClamAV did not return a valid scan result.");
    }
}
