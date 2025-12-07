using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Factory for creating CloudEvents with BeaconTower conventions.
/// </summary>
public class CloudEventFactory : ICloudEventFactory
{
    private readonly CloudEventFactoryOptions _options;
    private readonly Uri _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventFactory"/> class.
    /// </summary>
    /// <param name="options">The factory options.</param>
    public CloudEventFactory(CloudEventFactoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ServiceName);

        _options = options;
        // Cache the source URI to avoid parsing on every event creation
        _source = new Uri($"//beacontower.cloud/{_options.ServiceName}", UriKind.RelativeOrAbsolute);
    }

    /// <inheritdoc />
    public CloudEvent CreateEntityEvent<TData>(
        string entityType,
        string action,
        string subject,
        TData data,
        string? correlationId = null,
        Guid? userId = null,
        string? userName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        // Create CloudEvent with registered BeaconTower extension attributes
        var cloudEvent = new CloudEvent(
            CloudEventsSpecVersion.V1_0,
            BeaconTowerCloudEventExtensionAttributes.AllAttributes)
        {
            Id = Guid.CreateVersion7().ToString(),
            Source = _source,
            Type = CloudEventTypeGenerator.Generate(entityType, action),
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Subject = subject,
            Data = data
        };

        // Set BeaconTower extension attributes using registered attribute definitions
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId] = correlationId;
        }

        if (userId.HasValue)
        {
            // Store as binary (16 bytes) for efficiency - no string allocation
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId] = userId.Value.ToByteArray();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName] = userName;
        }

        return cloudEvent;
    }

    /// <inheritdoc />
    public CloudEvent CreateEvent<TData>(
        string type,
        string subject,
        TData data,
        string? correlationId = null,
        Guid? userId = null,
        string? userName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        // Create CloudEvent with registered BeaconTower extension attributes
        var cloudEvent = new CloudEvent(
            CloudEventsSpecVersion.V1_0,
            BeaconTowerCloudEventExtensionAttributes.AllAttributes)
        {
            Id = Guid.CreateVersion7().ToString(),
            Source = _source,
            Type = type,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Subject = subject,
            Data = data
        };

        // Set BeaconTower extension attributes using registered attribute definitions
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId] = correlationId;
        }

        if (userId.HasValue)
        {
            // Store as binary (16 bytes) for efficiency - no string allocation
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId] = userId.Value.ToByteArray();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName] = userName;
        }

        return cloudEvent;
    }
}

/// <summary>
/// Options for the CloudEventFactory.
/// </summary>
public class CloudEventFactoryOptions
{
    /// <summary>
    /// Gets or sets the service name used in the CloudEvent source URI.
    /// Example: "device-api" results in source "//beacontower.cloud/device-api".
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Interface for creating CloudEvents with BeaconTower conventions.
/// </summary>
public interface ICloudEventFactory
{
    /// <summary>
    /// Creates a CloudEvent for an entity operation (CRUD).
    /// </summary>
    /// <typeparam name="TData">The type of the event data.</typeparam>
    /// <param name="entityType">The entity type (e.g., "device").</param>
    /// <param name="action">The action (e.g., "created", "updated", "deleted").</param>
    /// <param name="subject">The subject identifier (typically entity ID).</param>
    /// <param name="data">The event data payload.</param>
    /// <param name="correlationId">Optional correlation ID for distributed tracing.</param>
    /// <param name="userId">Optional user ID who triggered the event.</param>
    /// <param name="userName">Optional user name who triggered the event.</param>
    /// <returns>A configured CloudEvent.</returns>
    public CloudEvent CreateEntityEvent<TData>(
        string entityType,
        string action,
        string subject,
        TData data,
        string? correlationId = null,
        Guid? userId = null,
        string? userName = null);

    /// <summary>
    /// Creates a CloudEvent with a custom type.
    /// </summary>
    /// <typeparam name="TData">The type of the event data.</typeparam>
    /// <param name="type">The CloudEvents type string.</param>
    /// <param name="subject">The subject identifier.</param>
    /// <param name="data">The event data payload.</param>
    /// <param name="correlationId">Optional correlation ID for distributed tracing.</param>
    /// <param name="userId">Optional user ID who triggered the event.</param>
    /// <param name="userName">Optional user name who triggered the event.</param>
    /// <returns>A configured CloudEvent.</returns>
    public CloudEvent CreateEvent<TData>(
        string type,
        string subject,
        TData data,
        string? correlationId = null,
        Guid? userId = null,
        string? userName = null);
}
