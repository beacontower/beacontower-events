using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Interface for publishing CloudEvents to the message broker.
/// </summary>
public interface ICloudEventPublisher
{
    /// <summary>
    /// Publishes a CloudEvent asynchronously.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PublishAsync(CloudEvent cloudEvent, CancellationToken ct = default);

    /// <summary>
    /// Publishes multiple CloudEvents as a batch asynchronously.
    /// </summary>
    /// <param name="cloudEvents">The CloudEvents to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PublishBatchAsync(IEnumerable<CloudEvent> cloudEvents, CancellationToken ct = default);
}
