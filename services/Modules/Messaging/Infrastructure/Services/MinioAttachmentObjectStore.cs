using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MinioAttachmentObjectStore(IMinioClient client,
    IOptions<AttachmentStorageOptions> options) : IAttachmentObjectStore
{
    public string BucketName => options.Value.Bucket;

    public async Task PutAsync(string objectKey, Stream content, long length, string contentType,
        CancellationToken cancellationToken)
    {
        await client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName).WithObject(objectKey).WithStreamData(content)
            .WithObjectSize(length).WithContentType(contentType), cancellationToken);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        await client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(BucketName).WithObject(objectKey), cancellationToken);
    }

    public async Task<byte[]> GetAsync(string objectKey, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(BucketName).WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(buffer)), cancellationToken);
        return buffer.ToArray();
    }
}
