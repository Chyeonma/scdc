namespace SCDC.Contracts.Identity;

// Identity owns eligibility, search keys and current session validation.
public interface IUserSearchDirectory
{
    Task<UserSearchResult> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken);
}

public sealed record UserSearchRequest(
    Guid ActorId, Guid SessionId, Guid SecurityStamp, string? Q, int Limit = 20, string? Cursor = null);
public sealed record UserSearchPage(IReadOnlyList<UserSummary> Items, string? NextCursor);
public sealed record UserSearchFailure(string Code, string Field, string Description);
public sealed record UserSearchResult(UserSearchPage? Page, UserSearchFailure? Failure);
