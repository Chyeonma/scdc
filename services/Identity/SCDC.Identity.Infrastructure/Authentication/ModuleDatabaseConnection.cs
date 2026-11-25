using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SCDC.Modules.Identity.Infrastructure;

public static class ModuleDatabaseConnection
{
    public static string Get(IConfiguration configuration, string module)
    {
        var value=configuration.GetConnectionString(module);
        if(string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"ConnectionStrings:{module} must be configured.");
        var connection=new NpgsqlConnectionStringBuilder(value);
        // Three independent pools share PostgreSQL's default 100 connections.
        // An explicit connection-string limit can override the local default.
        if(!connection.ShouldSerialize("Maximum Pool Size")) connection.MaxPoolSize=20;
        return connection.ConnectionString;
    }
}
