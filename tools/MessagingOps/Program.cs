using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Modules.Messaging;
using SCDC.Modules.Messaging.Application;

if (args.Length < 3 || args[1] != "--database"
    || args[0] is not ("list-failures" or "replay"))
{
    Console.Error.WriteLine("Usage: MessagingOps list-failures --database DB [--limit N] | replay --database DB --event UUID");
    return 2;
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("ConnectionStrings__Database must be supplied by the operator environment.");
    return 2;
}
var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
if (!string.Equals(database, args[2], StringComparison.Ordinal))
{
    Console.Error.WriteLine("The explicitly named database does not match ConnectionStrings__Database.");
    return 2;
}

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Database"] = connectionString,
    ["Modules:Messaging:Outbox:Enabled"] = "false",
    ["Modules:Messaging:Outbox:MaxAttempts"] = Environment.GetEnvironmentVariable("SCDC_OUTBOX_MAX_ATTEMPTS") ?? "10",
    ["Modules:Messaging:Attachments:Endpoint"] = "http://localhost:9000",
    ["Modules:Messaging:Attachments:AccessKey"] = "unused",
    ["Modules:Messaging:Attachments:SecretKey"] = "unused",
    ["Modules:Messaging:Attachments:Bucket"] = "unused",
    ["Modules:Messaging:Attachments:ClamAvHost"] = "localhost"
}).Build();
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(TimeProvider.System);
services.AddMessagingModule(configuration);
services.AddScoped<IRealtimeMessagePublisher, NoopPublisher>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<IMessagingOutboxDispatcher>();

if (args[0] == "list-failures")
{
    if (args.Length != 3 && (args.Length != 5 || args[3] != "--limit"
        || !int.TryParse(args[4], out _)))
    {
        Console.Error.WriteLine("list-failures accepts only --limit N (1-100).");
        return 2;
    }
    var limit = args.Length == 5 ? Math.Clamp(int.Parse(args[4]), 1, 100) : 20;
    foreach (var failure in await dispatcher.ListFailuresAsync(limit, CancellationToken.None))
        Console.WriteLine($"event={failure.EventId:D} type={failure.EventType} message={failure.AggregateId:D} space={failure.SpaceId:D} attempts={failure.AttemptCount} available={failure.AvailableAt:O}");
    return 0;
}

if (args.Length != 5 || args[3] != "--event" || !Guid.TryParse(args[4], out var eventId))
{
    Console.Error.WriteLine("Replay requires --event UUID.");
    return 2;
}
var replayed = await dispatcher.ReplayAsync(eventId, CancellationToken.None);
Console.WriteLine(replayed ? $"Replay scheduled for event {eventId:D}." : "Event was not replayable.");
return replayed ? 0 : 1;

internal sealed class NoopPublisher : IRealtimeMessagePublisher
{
    public Task PublishMessageCreatedAsync(RealtimeMessageCreated notification, CancellationToken cancellationToken) =>
        throw new NotSupportedException("MessagingOps never dispatches events.");

    public Task PublishMessageChangedAsync(RealtimeMessageChanged notification, CancellationToken cancellationToken) =>
        throw new NotSupportedException("MessagingOps never dispatches events.");
}
