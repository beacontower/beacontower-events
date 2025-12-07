using System.Collections.Concurrent;
using BeaconTower.Events.Abstractions;
using CloudNative.CloudEvents;

namespace BeaconTower.Events.InMemory;

/// <summary>
/// In-memory implementation of <see cref="ICloudEventPublisher"/> for unit testing.
/// Stores published CloudEvents in memory for inspection.
/// </summary>
public class InMemoryCloudEventPublisher : ICloudEventPublisher
{
    private readonly ConcurrentQueue<CloudEvent> _events = new();

    /// <summary>
    /// Gets all published CloudEvents in order of publication.
    /// </summary>
    public IReadOnlyList<CloudEvent> PublishedEvents => _events.ToArray();

    /// <inheritdoc />
    public Task PublishAsync(CloudEvent cloudEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        _events.Enqueue(cloudEvent);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishBatchAsync(IEnumerable<CloudEvent> cloudEvents, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cloudEvents);
        foreach (var cloudEvent in cloudEvents)
        {
            _events.Enqueue(cloudEvent);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all stored CloudEvents.
    /// </summary>
    public void ClearEvents()
    {
        _events.Clear();
    }
}
