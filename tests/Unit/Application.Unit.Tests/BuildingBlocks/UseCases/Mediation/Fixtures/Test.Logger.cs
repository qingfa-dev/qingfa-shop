using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Application.Unit.Tests.BuildingBlocks.UseCases.Mediation.Fixtures;

public class TestLogger<T> : ILogger<T>
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => _enabledLevels.TryGetValue(logLevel, out var enabled) ? enabled : true;

    private readonly ConcurrentDictionary<LogLevel, bool> _enabledLevels = new();

    public void SetEnabled(LogLevel logLevel, bool enabled)
    {
        _enabledLevels[logLevel] = enabled;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Enqueue(new LogEntry
        {
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception
        });
    }

    public record LogEntry
    {
        public LogLevel Level { get; init; }
        public EventId EventId { get; init; }
        public string Message { get; init; } = default!;
        public Exception? Exception { get; init; }
    }
}