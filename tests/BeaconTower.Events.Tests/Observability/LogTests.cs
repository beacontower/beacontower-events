using BeaconTower.Events.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BeaconTower.Events.Tests.Observability;

/// <summary>
/// Tests for high-performance logging via Log class source generators.
/// </summary>
public sealed class LogTests
{
    private readonly TestLogger _logger;

    public LogTests()
    {
        _logger = new TestLogger();
    }

    [Fact]
    public void Publishing_Logs_CloudEvent_Type_And_Subject_At_Debug_Level()
    {
        // Arrange
        var eventType = "cloud.beacontower.device.created";
        var subject = "beacontower.device.created";

        // Act
        Log.Publishing(_logger, eventType, subject);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Debug);
        _logger.LastEventId.Should().Be(2001);
        _logger.LastMessage.Should().Contain(eventType);
        _logger.LastMessage.Should().Contain(subject);
    }

    [Fact]
    public void Published_Logs_Event_Id_And_Subject_At_Debug_Level()
    {
        // Arrange
        var eventId = "evt-123";
        var subject = "beacontower.device.created";
        var seq = 42UL;

        // Act
        Log.Published(_logger, eventId, subject, seq);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Debug);
        _logger.LastEventId.Should().Be(2002);
        _logger.LastMessage.Should().Contain(eventId);
        _logger.LastMessage.Should().Contain(subject);
        _logger.LastMessage.Should().Contain(seq.ToString());
    }

    [Fact]
    public void Processing_Logs_Event_At_Debug_Level()
    {
        // Arrange
        var eventId = "evt-123";
        var eventType = "cloud.beacontower.device.created";

        // Act
        Log.Processing(_logger, eventId, eventType);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Debug);
        _logger.LastEventId.Should().Be(2003);
        _logger.LastMessage.Should().Contain(eventId);
        _logger.LastMessage.Should().Contain(eventType);
    }

    [Fact]
    public void Handler_Completed_Logs_Duration_At_Debug_Level()
    {
        // Arrange
        var eventId = "evt-123";
        var durationMs = 45.5;

        // Act
        Log.HandlerCompleted(_logger, eventId, durationMs);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Debug);
        _logger.LastEventId.Should().Be(2004);
        _logger.LastMessage.Should().Contain(eventId);
        _logger.LastMessage.Should().Contain("45.5");
    }

    [Fact]
    public void Handler_Failed_Routing_To_DLQ_Logs_At_Error_Level()
    {
        // Arrange
        var eventId = "evt-123";
        var maxAttempts = 3;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Log.HandlerFailedRoutingToDlq(_logger, exception, eventId, maxAttempts);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Error);
        _logger.LastEventId.Should().Be(4001);
        _logger.LastMessage.Should().Contain(eventId);
        _logger.LastMessage.Should().Contain(maxAttempts.ToString());
        _logger.LastException.Should().Be(exception);
    }

    [Fact]
    public void Handler_Failed_Will_Retry_Logs_At_Warning_Level()
    {
        // Arrange
        var eventId = "evt-123";
        var attempt = 1;
        var maxAttempts = 3;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Log.HandlerFailedWillRetry(_logger, exception, eventId, attempt, maxAttempts);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Warning);
        _logger.LastEventId.Should().Be(3002);
        _logger.LastMessage.Should().Contain(eventId);
        _logger.LastException.Should().Be(exception);
    }

    [Fact]
    public void Publish_Retry_Logs_At_Warning_Level()
    {
        // Arrange
        var attempt = 2;
        var delayMs = 200.0;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Log.PublishRetry(_logger, exception, attempt, delayMs);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Warning);
        _logger.LastEventId.Should().Be(3001);
        _logger.LastMessage.Should().Contain(attempt.ToString());
    }

    [Fact]
    public void Creating_Stream_Logs_At_Information_Level()
    {
        // Arrange
        var streamName = "beacontower-events";
        var subjects = "beacontower.>";

        // Act
        Log.CreatingStream(_logger, streamName, subjects);

        // Assert
        _logger.LastLogLevel.Should().Be(LogLevel.Information);
        _logger.LastEventId.Should().Be(1002);
        _logger.LastMessage.Should().Contain(streamName);
        _logger.LastMessage.Should().Contain(subjects);
    }

    /// <summary>
    /// Test logger that captures the last log entry for verification.
    /// </summary>
    private sealed class TestLogger : ILogger
    {
        public LogLevel LastLogLevel { get; private set; }
        public int LastEventId { get; private set; }
        public string LastMessage { get; private set; } = string.Empty;
        public Exception? LastException { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LastLogLevel = logLevel;
            LastEventId = eventId.Id;
            LastMessage = formatter(state, exception);
            LastException = exception;
        }
    }
}
