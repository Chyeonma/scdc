namespace SCDC.Modules.Community.Features.Permissions.Domain;

internal sealed class Role
{
    public Guid Id
    {
        get; init;
    }
    public Guid ServerId
    {
        get; init;
    }
    public string Name { get; init; } = "@everyone";
    public bool IsDefault { get; init; } = true;
    public bool IsSystem { get; init; } = true;
    public DateTimeOffset CreatedAt
    {
        get; init;
    }
    public DateTimeOffset UpdatedAt
    {
        get; init;
    }
}
