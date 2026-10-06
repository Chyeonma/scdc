namespace SCDC.Modules.Community.Features.Memberships.Domain;

internal sealed class Membership
{
    public Guid ServerId
    {
        get; init;
    }
    public Guid UserId
    {
        get; init;
    }
    public Guid MembershipId
    {
        get; init;
    }
    public short Status { get; init; } = 1;
    public DateTimeOffset JoinedAt
    {
        get; init;
    }
    public DateTimeOffset? LeftAt
    {
        get; init;
    }
    public int Version { get; init; } = 1;
}
