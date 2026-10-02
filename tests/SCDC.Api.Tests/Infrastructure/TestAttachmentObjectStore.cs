using System.Collections.Concurrent;
using System.Text;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class TestAttachmentObjectStore : IAttachmentObjectStore
{
    public string BucketName => "test-attachments";
    public ConcurrentDictionary<string, byte[]> Objects { get; } = new();

    public async Task PutAsync(string objectKey, Stream content, long length, string contentType,
        CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        Objects[objectKey] = copy.ToArray();
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        Objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task<byte[]> GetAsync(string objectKey, CancellationToken cancellationToken) =>
        Task.FromResult(Objects.TryGetValue(objectKey, out var bytes)
            ? bytes : throw new FileNotFoundException("Attachment object is missing.", objectKey));
}

public sealed class TestFileScanner : IFileScanner
{
    public Task<bool> IsCleanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(content.Span);
        if (text.Contains("SCAN_ERROR", StringComparison.Ordinal)) throw new IOException("Scanner unavailable");
        return Task.FromResult(!text.Contains("EICAR", StringComparison.Ordinal));
    }
}
