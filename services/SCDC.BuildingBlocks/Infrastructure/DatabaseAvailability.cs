using Npgsql;

namespace SCDC.BuildingBlocks.Infrastructure;

public static class DatabaseAvailability
{
    public static bool IsUnavailable(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is NpgsqlException { IsTransient: true }
                || cause is PostgresException { SqlState: "58000" }) return true;
        return false;
    }
}
