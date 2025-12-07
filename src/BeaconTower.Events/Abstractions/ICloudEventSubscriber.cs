using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Interface for subscribing to CloudEvents from the message broker.
/// </summary>
public interface ICloudEventSubscriber
{
    /// <summary>
    /// Subscribes to CloudEvents matching the specified subject pattern.
    /// </summary>
    /// <typeparam name="TData">The type of the event data payload.</typeparam>
    /// <param name="subjectPattern">
    /// The subject pattern to subscribe to. Supports wildcards:
    /// - * matches a single token (e.g., "beacontower.*.created")
    /// - > matches multiple tokens (e.g., "beacontower.>")
    /// </param>
    /// <param name="handler">The handler to invoke for matching CloudEvents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous subscription operation.</returns>
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        ICloudEventHandler<TData> handler,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes to CloudEvents matching the specified subject pattern using a delegate handler.
    /// </summary>
    /// <typeparam name="TData">The type of the event data payload.</typeparam>
    /// <param name="subjectPattern">The subject pattern to subscribe to.</param>
    /// <param name="handler">The delegate handler to invoke for matching CloudEvents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous subscription operation.</returns>
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        Func<CloudEvent, TData?, CancellationToken, Task> handler,
        CancellationToken ct = default);
}
