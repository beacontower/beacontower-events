namespace BeaconTower.Events.Nats;

/// <summary>
/// Options for configuring the NATS CloudEvent publisher.
/// </summary>
public class NatsCloudEventPublisherOptions
{
    /// <summary>
    /// Gets or sets the NATS connection string.
    /// Default: "nats://localhost:4222"
    /// </summary>
    public string ConnectionString { get; set; } = "nats://localhost:4222";

    /// <summary>
    /// Gets or sets the JetStream stream name for events.
    /// Default: "beacontower-events"
    /// </summary>
    public string StreamName { get; set; } = "beacontower-events";

    /// <summary>
    /// Gets or sets the subjects the stream should capture.
    /// Default: ["beacontower.>"] (all beacontower events)
    /// </summary>
    public IReadOnlyList<string> StreamSubjects { get; set; } = ["beacontower.>"];

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay for retry backoff.
    /// Default: 100ms
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}
