namespace SCDC.Contracts.Identity;

public interface IUserDirectory
{
    Task<UserSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserSummary?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, UserSummary>> FindByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}

public sealed record UserSummary(Guid Id, string Username, string DisplayName,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    string? Availability = null);
