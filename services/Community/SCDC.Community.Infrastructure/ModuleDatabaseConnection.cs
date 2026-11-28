using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SCDC.Modules.Community.Infrastructure;

public static class ModuleDatabaseConnection
{
    public static string Get(IConfiguration configuration)
    {
        var value=configuration.GetConnectionString("Community");
        if(string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("ConnectionStrings:Community must be configured.");
        var connection=new NpgsqlConnectionStringBuilder(value);
        // Bound this owner's pool on the shared PostgreSQL instance.
        // An explicit connection-string limit can override the local default.
        if(!connection.ShouldSerialize("Maximum Pool Size")) connection.MaxPoolSize=20;
        return connection.ConnectionString;
    }
}
