namespace SCDC.Modules.Community.Features.Servers.Domain;

internal sealed class Server
{
    public Guid Id
    {
        get; init;
    }
    public Guid OwnerUserId
    {
        get; init;
    }
    public required string Name
    {
        get; init;
    }
    public required string Slug
    {
        get; init;
    }
    public string? Description
    {
        get; init;
    }
    public short Visibility
    {
        get; init;
    }
    public short JoinMode { get; init; } = 1;
    public short Status { get; init; } = 1;
    public int Version { get; init; } = 1;
    public int AccessVersion { get; init; } = 1;
    public DateTimeOffset CreatedAt
    {
        get; init;
    }
    public DateTimeOffset UpdatedAt
    {
        get; init;
    }
}
