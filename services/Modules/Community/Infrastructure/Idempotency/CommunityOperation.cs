namespace SCDC.Modules.Community.Infrastructure.Idempotency;

internal sealed class CommunityOperation
{
    public Guid ActorUserId
    {
        get; init;
    }
    public string Kind { get; init; } = "create_server";
    public Guid ScopeId
    {
        get; init;
    }
    public Guid ClientOperationId
    {
        get; init;
    }
    public short FingerprintVersion { get; init; } = 1;
    public required string KeyId
    {
        get; init;
    }
    public required byte[] Fingerprint
    {
        get; init;
    }
    public Guid ResourceId
    {
        get; init;
    }
    public DateTimeOffset CreatedAt
    {
        get; init;
    }
}
