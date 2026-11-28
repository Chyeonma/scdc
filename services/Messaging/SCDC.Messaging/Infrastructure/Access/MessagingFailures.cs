using Npgsql;
using SCDC.BuildingBlocks.Infrastructure;
namespace SCDC.Modules.Messaging.Infrastructure.Access;

internal static class MessagingFailures
{
    public static bool Temporary(Exception failure)
    {
        if (DatabaseAvailability.IsUnavailable(failure)) return true;
        for (Exception? cause = failure; cause is not null; cause = cause.InnerException)
            if (cause is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "57014" or "25P04" }) return true;
        return false;
    }
}
