using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class TestLogCaptureProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();
    public IReadOnlyCollection<string> Entries => _entries.ToArray();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(_entries);
    public void Dispose() { }

    private sealed class CaptureLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Scope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(formatter(state, exception) + exception);
    }

    private sealed class Scope : IDisposable
    {
        public static Scope Instance { get; } = new();
        public void Dispose() { }
    }
}
