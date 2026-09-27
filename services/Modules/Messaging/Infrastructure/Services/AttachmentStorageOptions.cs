namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class AttachmentStorageOptions
{
    public const string SectionName = "Modules:Messaging:Attachments";
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string ClamAvHost { get; set; } = string.Empty;
    public int ClamAvPort { get; set; } = 3310;
}
