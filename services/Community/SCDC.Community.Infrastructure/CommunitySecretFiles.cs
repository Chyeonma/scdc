using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SCDC.Modules.Community.Infrastructure;

public static class CommunitySecretFiles
{
    public static void AddCommunitySecretFiles(this IConfigurationBuilder builder)
    {
        var configuration = builder.Build();
        var values = new Dictionary<string, string?>();
        if (configuration["ConnectionStrings:CommunityPasswordFile"] is { Length: > 0 } passwordFile)
        {
            var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Community"));
            connection.Password = Read(passwordFile);
            values["ConnectionStrings:Community"] = connection.ConnectionString;
        }
        foreach (var key in configuration.GetSection("Modules:Community:Operations:KeyFiles").GetChildren())
        {
            if (string.IsNullOrWhiteSpace(key.Value)) throw new InvalidOperationException("Community operation key file is required.");
            values[$"Modules:Community:Operations:Keys:{key.Key}"] = Read(key.Value);
        }
        builder.AddInMemoryCollection(values);
    }

    private static string Read(string path)
    {
        var value = File.ReadAllText(path).Trim();
        if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("Community secret file is empty.");
        return value;
    }
}
