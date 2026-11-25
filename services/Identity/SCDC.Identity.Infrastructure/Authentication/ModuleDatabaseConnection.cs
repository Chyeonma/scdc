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
        var passwordFile=configuration["ConnectionStrings:"+module+"PasswordFile"];
        if(!string.IsNullOrWhiteSpace(passwordFile))
        {
            if(!string.IsNullOrEmpty(connection.Password))
                throw new InvalidOperationException("Configure either a database password file or an inline password.");
            connection.Password=File.ReadAllText(passwordFile).TrimEnd('\r','\n');
            if(string.IsNullOrEmpty(connection.Password))
                throw new InvalidOperationException("Database password file must not be empty.");
        }
        // Keep a bounded pool for the Identity host and its workers.
        if(!connection.ShouldSerialize("Maximum Pool Size")) connection.MaxPoolSize=20;
        return connection.ConnectionString;
    }
}
