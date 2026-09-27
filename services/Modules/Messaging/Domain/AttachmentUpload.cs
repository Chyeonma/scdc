namespace SCDC.Modules.Messaging.Domain;

internal sealed class AttachmentUpload
{
    public Guid Id { get; set; }
    public Guid ClientUploadId { get; set; }
    public Guid SpaceId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public short ScanStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid? AttachedMessageId { get; set; }
}
