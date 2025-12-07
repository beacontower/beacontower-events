using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Interface for handling CloudEvents with typed data payloads.
/// </summary>
/// <typeparam name="TData">The type of the event data payload.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - EventHandler is intentional for this domain
public interface ICloudEventHandler<in TData>
#pragma warning restore CA1711
{
    /// <summary>
    /// Handles a CloudEvent asynchronously.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent envelope containing metadata.</param>
    /// <param name="data">The deserialized data payload, or null if deserialization failed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task HandleAsync(CloudEvent cloudEvent, TData? data, CancellationToken ct = default);
}
