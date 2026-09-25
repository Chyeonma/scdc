namespace SCDC.Modules.Messaging.Application;

public interface IAttachmentObjectStore
{
    string BucketName { get; }
    Task PutAsync(string objectKey, Stream content, long length, string contentType,
        CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
