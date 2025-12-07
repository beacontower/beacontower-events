using System.Diagnostics;
using System.Diagnostics.Metrics;
using BeaconTower.Observability;

namespace BeaconTower.Events.Observability;

/// <summary>
/// Provides metrics instrumentation for CloudEvents publishing and processing.
/// </summary>
/// <remarks>
/// Exposes:
/// - events_published_total: Counter of successfully published events (by type)
/// - events_processing_duration_seconds: Histogram of handler execution time
/// </remarks>
public sealed class CloudEventsMetrics : IDisposable
{
    /// <summary>
    /// The meter name used for CloudEvents metrics.
    /// </summary>
    public const string MeterName = "BeaconTower.Events";

    private readonly Meter _meter;
    private readonly Counter<long> _eventsPublishedCounter;
    private readonly Histogram<double> _processingDurationHistogram;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventsMetrics"/> class.
    /// </summary>
    public CloudEventsMetrics()
    {
        _meter = MeterFactory.Create("Events");

        _eventsPublishedCounter = _meter.CreateCounter<long>(
            "events_published_total",
            unit: "{event}",
            description: "Total number of CloudEvents successfully published");

        _processingDurationHistogram = _meter.CreateHistogram<double>(
            "events_processing_duration_seconds",
            unit: "s",
            description: "Duration of CloudEvent handler execution in seconds");
    }

    /// <summary>
    /// Records a successful event publication.
    /// </summary>
    /// <param name="eventType">The CloudEvent type that was published.</param>
    public void RecordEventPublished(string eventType)
    {
        _eventsPublishedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records the duration of handler execution.
    /// </summary>
    /// <param name="eventType">The CloudEvent type being processed.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="success">Whether the handler completed successfully.</param>
    public void RecordProcessingDuration(string eventType, TimeSpan duration, bool success)
    {
        _processingDurationHistogram.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("success", success));
    }

    /// <summary>
    /// Creates a stopwatch for timing operations.
    /// </summary>
    /// <returns>A started <see cref="Stopwatch"/>.</returns>
    public static Stopwatch StartTimer() => Stopwatch.StartNew();

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
