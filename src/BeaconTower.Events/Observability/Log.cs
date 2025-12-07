using BeaconTower.Observability;
using Microsoft.Extensions.Logging;

namespace BeaconTower.Events.Observability;

/// <summary>
/// High-performance logging using source generators.
/// Zero cost when log level is disabled.
/// </summary>
/// <remarks>
/// Event ID ranges (from <see cref="LogEventIds"/>):
/// - 1xxx: INFO - Operation lifecycle
/// - 2xxx: DEBUG - Event details for production debugging
/// - 3xxx: WARNING - Degraded performance or retries
/// - 4xxx: ERROR - Failures requiring attention
/// </remarks>
public static partial class Log
{
    // INFO - Operation lifecycle (1xxx)
    [LoggerMessage(Level = LogLevel.Information, EventId = LogEventIds.InfoBase + 1,
        Message = "Creating durable consumer {ConsumerName} for subject {SubjectPattern}")]
    public static partial void CreatingDurableConsumer(ILogger logger, string consumerName, string subjectPattern);

    [LoggerMessage(Level = LogLevel.Information, EventId = LogEventIds.InfoBase + 2,
        Message = "Stream {StreamName} not found, creating with subjects {Subjects}")]
    public static partial void CreatingStream(ILogger logger, string streamName, string subjects);

    [LoggerMessage(Level = LogLevel.Information, EventId = LogEventIds.InfoBase + 3,
        Message = "Routed CloudEvent {EventId} to DLQ {DlqSubject}")]
    public static partial void RoutedToDlq(ILogger logger, string eventId, string dlqSubject);

    // DEBUG - Event details for production debugging (2xxx)
    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 1,
        Message = "Publishing CloudEvent Type={EventType} to Subject={Subject}")]
    public static partial void Publishing(ILogger logger, string eventType, string subject);

    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 2,
        Message = "Published CloudEvent Id={EventId} to {Subject}, Seq={Seq}")]
    public static partial void Published(ILogger logger, string eventId, string subject, ulong seq);

    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 3,
        Message = "Processing CloudEvent Id={EventId} Type={EventType}")]
    public static partial void Processing(ILogger logger, string eventId, string eventType);

    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 4,
        Message = "Handler completed for CloudEvent Id={EventId} in {DurationMs:F1}ms")]
    public static partial void HandlerCompleted(ILogger logger, string eventId, double durationMs);

    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 5,
        Message = "Starting message processing loop for {SubjectPattern}")]
    public static partial void StartingProcessingLoop(ILogger logger, string subjectPattern);

    [LoggerMessage(Level = LogLevel.Debug, EventId = LogEventIds.DebugBase + 6,
        Message = "Message processing cancelled for {SubjectPattern}")]
    public static partial void ProcessingLoopCancelled(ILogger logger, string subjectPattern);

    // WARNING - Degraded performance or retries (3xxx)
    [LoggerMessage(Level = LogLevel.Warning, EventId = LogEventIds.WarningBase + 1,
        Message = "Retry attempt {Attempt} for NATS publish after {DelayMs:F0}ms")]
    public static partial void PublishRetry(ILogger logger, Exception? ex, int attempt, double delayMs);

    [LoggerMessage(Level = LogLevel.Warning, EventId = LogEventIds.WarningBase + 2,
        Message = "Handler failed for CloudEvent Id={EventId}, attempt {Attempt}/{MaxAttempts}, NAKing for redelivery")]
    public static partial void HandlerFailedWillRetry(ILogger logger, Exception ex, string eventId, int attempt, int maxAttempts);

    // ERROR - Failures requiring attention (4xxx)
    [LoggerMessage(Level = LogLevel.Error, EventId = LogEventIds.ErrorBase + 1,
        Message = "Handler failed for CloudEvent Id={EventId} after {MaxAttempts} attempts, routing to DLQ")]
    public static partial void HandlerFailedRoutingToDlq(ILogger logger, Exception ex, string eventId, int maxAttempts);

    [LoggerMessage(Level = LogLevel.Error, EventId = LogEventIds.ErrorBase + 2,
        Message = "Failed to route CloudEvent Id={EventId} to DLQ {DlqSubject}")]
    public static partial void DlqRoutingFailed(ILogger logger, Exception ex, string eventId, string dlqSubject);

    [LoggerMessage(Level = LogLevel.Error, EventId = LogEventIds.ErrorBase + 3,
        Message = "NATS API error in message processing loop for {SubjectPattern}")]
    public static partial void NatsApiError(ILogger logger, Exception ex, string subjectPattern);

    [LoggerMessage(Level = LogLevel.Error, EventId = LogEventIds.ErrorBase + 4,
        Message = "NATS error in message processing loop for {SubjectPattern}")]
    public static partial void NatsError(ILogger logger, Exception ex, string subjectPattern);
}
