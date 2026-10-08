using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class UserSearchDirectory(
    IdentityDbContext db, IDataProtectionProvider protection, TimeProvider clock) : IUserSearchDirectory
{
    private readonly IDataProtector _protector = protection.CreateProtector("Messaging.UserSearch.v1");
    private sealed record Position(int Rank, string Username, Guid Id);
    private sealed record Cursor(int Version, Guid ActorId, string Q, int Limit, Position Position, DateTimeOffset ExpiresAt);

    public async Task<UserSearchResult> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        // Authentication is also checked by JWT middleware. Recheck here because this
        // directory may be used through Contracts without an HTTP controller.
        if (!await db.AuthSessions.AsNoTracking().AnyAsync(s =>
            s.Id == request.SessionId && s.UserId == request.ActorId && s.RevokedAt == null && s.ExpiresAt > now
            && s.User.Status == UserStatus.Active && s.User.DeletedAt == null
            && s.User.SecurityState != null && s.User.SecurityState.SecurityStamp == request.SecurityStamp
            && s.User.Emails.Any(e => e.IsPrimary && e.VerifiedAt != null), cancellationToken))
            return Fail("Common.Unauthorized", "", "The current session is not available.");

        string q;
        try { q = UserSearchKey.Normalize(request.Q ?? ""); }
        catch (ArgumentException) { return Invalid("q", "Search text must be valid Unicode."); }
        if (q.Length is < 2 or > 64 || q.Contains('\0')) return Invalid("q", "Use 2–64 UTF-16 units after trim/NFC.");
        if (request.Limit is < 1 or > 50) return Invalid("limit", "Use a page size from 1 to 50.");

        Cursor? cursor = null;
        if (request.Cursor is not null)
        {
            if (request.Cursor.Length is < 1 or > 4096) return BadCursor();
            try
            {
                cursor = JsonSerializer.Deserialize<Cursor>(_protector.Unprotect(request.Cursor));
                if (cursor is null || cursor.Version != 1 || cursor.ActorId != request.ActorId
                    || cursor.Q != q || cursor.Limit != request.Limit || cursor.ExpiresAt <= now
                    || cursor.Position is null || cursor.Position.Rank is < 0 or > 1
                    || string.IsNullOrEmpty(cursor.Position.Username) || cursor.Position.Id == Guid.Empty)
                    return BadCursor();
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
            { return BadCursor(); }
        }

        var pattern = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var query = db.Users.AsNoTracking()
            .Where(u => u.Id != request.ActorId && u.Status == UserStatus.Active && u.DeletedAt == null
                && u.Emails.Any(e => e.IsPrimary && e.VerifiedAt != null) && u.Profile != null)
            .Where(u => EF.Functions.Like(EF.Functions.Collate(u.NormalizedUsername, "C"), pattern, "\\")
                || EF.Functions.Like(EF.Functions.Collate(u.Profile!.DisplayNameSearchKey, "C"), pattern, "\\"))
            .Select(u => new { u.Id, u.Username, u.NormalizedUsername, u.Profile!.DisplayName,
                Rank = u.NormalizedUsername == q ? 0 : 1 });
        if (cursor is not null)
        {
            var position = cursor.Position;
            query = query.Where(u => u.Rank > position.Rank || (u.Rank == position.Rank
                && (string.Compare(EF.Functions.Collate(u.NormalizedUsername, "C"), position.Username) > 0
                    || (u.NormalizedUsername == position.Username && u.Id.CompareTo(position.Id) > 0))));
        }
        var rows = await query.OrderBy(u => u.Rank)
            .ThenBy(u => EF.Functions.Collate(u.NormalizedUsername, "C")).ThenBy(u => u.Id)
            .Take(request.Limit + 1).ToListAsync(cancellationToken);
        var page = rows.Take(request.Limit).ToArray();
        string? nextCursor = null;
        if (rows.Count > request.Limit)
        {
            var last = page[^1];
            var next = new Cursor(1, request.ActorId, q, request.Limit,
                new Position(last.Rank, last.NormalizedUsername, last.Id), cursor?.ExpiresAt ?? now.AddHours(24));
            nextCursor = _protector.Protect(JsonSerializer.Serialize(next));
        }
        return new(new UserSearchPage(page.Select(u => new UserSummary(u.Id, u.Username, u.DisplayName)).ToArray(), nextCursor), null);
    }

    private static UserSearchResult Fail(string code, string field, string description) => new(null, new(code, field, description));
    private static UserSearchResult Invalid(string field, string description) => Fail("Common.ValidationFailed", field, description);
    private static UserSearchResult BadCursor() => Fail("CURSOR_INVALID", "cursor", "The search cursor is invalid or expired; restart this search.");
}
