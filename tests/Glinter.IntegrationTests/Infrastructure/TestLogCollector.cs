using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Glinter.IntegrationTests.Infrastructure;

public sealed class TestLogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _messages = new();

    public IReadOnlyCollection<string> Messages => _messages.ToArray();

    public ILogger CreateLogger(string categoryName) => new CollectorLogger(_messages);

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
    }

    private sealed class CollectorLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages;

        public CollectorLogger(ConcurrentQueue<string> messages)
        {
            _messages = messages;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Enqueue(formatter(state, exception));
        }
    }
}
