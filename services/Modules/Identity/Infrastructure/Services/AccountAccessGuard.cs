using System.Data.Common;
using Npgsql;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class AccountAccessGuard(TimeProvider timeProvider) : IAccountAccessGuard
{
    public async Task<AccountAccessCheck> AcquireAsync(
        AccountActor actor, DbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is not NpgsqlTransaction { Connection: not null } postgresTransaction)
        {
            throw new InvalidOperationException("Account access requires the caller's PostgreSQL transaction.");
        }

        var connection = postgresTransaction.Connection;
        await using var userLock = new NpgsqlCommand("SELECT id FROM identity.users WHERE id = @user FOR SHARE", connection, postgresTransaction)
        { CommandTimeout = 5 };
        userLock.Parameters.AddWithValue("user", actor.UserId);
        if (await userLock.ExecuteScalarAsync(cancellationToken) is null)
        {
            return new(AccountAccessStatus.InvalidSession);
        }

        // All Identity security writers acquire LockUserAsync before touching the
        // user's email, stamp or sessions. The share lock protects that protocol.
        await using var command = new NpgsqlCommand("""
            SELECT u.status = 1 AND u.deleted_at IS NULL,
                   EXISTS (SELECT 1 FROM identity.user_emails e
                           WHERE e.user_id = u.id AND e.is_primary AND e.verified_at IS NOT NULL),
                   s.revoked_at, s.expires_at, security.security_stamp
            FROM identity.users u
            LEFT JOIN identity.auth_sessions s ON s.user_id = u.id AND s.id = @session
            LEFT JOIN identity.user_security_states security ON security.user_id = u.id
            WHERE u.id = @user
            """, connection, postgresTransaction) { CommandTimeout = 5 };
        command.Parameters.AddWithValue("user", actor.UserId);
        command.Parameters.AddWithValue("session", actor.SessionId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)
            || reader.IsDBNull(3) || reader.IsDBNull(4)
            || !reader.IsDBNull(2) || reader.GetGuid(4) != actor.SecurityStamp)
        {
            return new(AccountAccessStatus.InvalidSession);
        }

        var expiresAt = reader.GetFieldValue<DateTimeOffset>(3);
        if (expiresAt <= timeProvider.GetUtcNow())
        {
            return new(AccountAccessStatus.InvalidSession);
        }

        return reader.GetBoolean(0) && reader.GetBoolean(1)
            ? new(AccountAccessStatus.Allowed, expiresAt)
            : new(AccountAccessStatus.AccountDenied);
    }
}
