using System.Diagnostics.Metrics;
using BeaconTower.Events.Observability;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.Observability;

/// <summary>
/// Tests for CloudEventsMetrics.
/// </summary>
public sealed class CloudEventsMetricsTests : IDisposable
{
    private readonly CloudEventsMetrics _metrics;
    private readonly MeterListener _listener;
    private readonly List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> _recordedMeasurements = [];

    public CloudEventsMetricsTests()
    {
        _metrics = new CloudEventsMetrics();
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == CloudEventsMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            _recordedMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _recordedMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _metrics.Dispose();
    }

    [Fact]
    public void Events_Published_Total_Counter_Increments_On_Successful_Publish()
    {
        // Arrange
        var eventType = "cloud.beacontower.device.created";

        // Act
        _metrics.RecordEventPublished(eventType);
        _metrics.RecordEventPublished(eventType);
        _metrics.RecordEventPublished("cloud.beacontower.device.updated");

        // Assert
        var counterMeasurements = _recordedMeasurements
            .Where(m => m.Name == "events_published_total")
            .ToList();

        counterMeasurements.Should().HaveCount(3);
        counterMeasurements[0].Value.Should().Be(1L);
        counterMeasurements[0].Tags.Should().Contain(t =>
            t.Key == "event_type" && Equals(t.Value, eventType));
    }

    [Fact]
    public void Events_Processing_Duration_Histogram_Records_Handler_Execution_Time()
    {
        // Arrange
        var eventType = "cloud.beacontower.device.created";
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        _metrics.RecordProcessingDuration(eventType, duration, success: true);

        // Assert
        var histogramMeasurements = _recordedMeasurements
            .Where(m => m.Name == "events_processing_duration_seconds")
            .ToList();

        histogramMeasurements.Should().ContainSingle();
        ((double)histogramMeasurements[0].Value).Should().BeApproximately(0.05, 0.001);
        histogramMeasurements[0].Tags.Should().Contain(t =>
            t.Key == "event_type" && Equals(t.Value, eventType));

        var successTag = histogramMeasurements[0].Tags.FirstOrDefault(t => t.Key == "success");
        successTag.Value.Should().Be(true);
    }

    [Fact]
    public void Events_Processing_Duration_Records_Failure_Status()
    {
        // Arrange
        var eventType = "cloud.beacontower.device.created";

        // Act
        _metrics.RecordProcessingDuration(eventType, TimeSpan.FromMilliseconds(10), success: false);

        // Assert
        var histogramMeasurements = _recordedMeasurements
            .Where(m => m.Name == "events_processing_duration_seconds")
            .ToList();

        histogramMeasurements.Should().ContainSingle();

        var successTag = histogramMeasurements[0].Tags.FirstOrDefault(t => t.Key == "success");
        successTag.Value.Should().Be(false);
    }

    [Fact]
    public void StartTimer_Returns_Started_Stopwatch()
    {
        // Act
        var stopwatch = CloudEventsMetrics.StartTimer();

        // Assert
        stopwatch.Should().NotBeNull();
        stopwatch.IsRunning.Should().BeTrue();
    }
}
