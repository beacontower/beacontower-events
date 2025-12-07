namespace BeaconTower.Events.Nats;

/// <summary>
/// Options for configuring the NATS CloudEvent subscriber.
/// </summary>
public class NatsCloudEventSubscriberOptions
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
    /// Gets or sets the service name used for consumer naming.
    /// This is used to create durable consumer names and consumer groups.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of concurrent handler executions.
    /// Default: 10
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts before routing to DLQ.
    /// Default: 3
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the acknowledgement wait time for messages.
    /// If the handler doesn't complete within this time, the message is redelivered.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan AckWait { get; set; } = TimeSpan.FromSeconds(30);
}
