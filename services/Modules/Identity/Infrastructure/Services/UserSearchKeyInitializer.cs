using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

// Explicit deployment command; not a startup migration, HTTP endpoint or search side effect.
internal sealed class UserSearchKeyInitializer(IdentityDbContext db)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var rows = await db.UserProfiles.FromSqlRaw("""
                SELECT * FROM identity.user_profiles WHERE display_name_search_key = ''
                ORDER BY user_id LIMIT 500 FOR UPDATE
                """).ToListAsync(cancellationToken);
            if (rows.Count == 0) { await transaction.CommitAsync(cancellationToken); break; }
            foreach (var row in rows) row.DisplayNameSearchKey = UserSearchKey.Normalize(row.DisplayName);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }
}
