using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Factory for creating CloudEvents with BeaconTower conventions.
/// </summary>
public class CloudEventFactory : ICloudEventFactory
{
    private readonly CloudEventFactoryOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventFactory"/> class.
    /// </summary>
    /// <param name="options">The factory options.</param>
    public CloudEventFactory(CloudEventFactoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ServiceName);

        _options = options;
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

        var cloudEvent = new CloudEvent
        {
            Id = Guid.CreateVersion7().ToString(),
            Source = new Uri($"//beacontower/{_options.ServiceName}", UriKind.RelativeOrAbsolute),
            Type = CloudEventTypeGenerator.Generate(entityType, action),
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Subject = subject,
            Data = data
        };

        // Add BeaconTower extension attributes (stored as typed objects, not strings)
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cloudEvent[CloudEventExtensions.CorrelationId] = correlationId;
        }

        if (userId.HasValue)
        {
            // CloudEvents extension attributes require string values for unregistered attributes
            cloudEvent[CloudEventExtensions.UserId] = userId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            cloudEvent[CloudEventExtensions.UserName] = userName;
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

        var cloudEvent = new CloudEvent
        {
            Id = Guid.CreateVersion7().ToString(),
            Source = new Uri($"//beacontower/{_options.ServiceName}", UriKind.RelativeOrAbsolute),
            Type = type,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Subject = subject,
            Data = data
        };

        // Add BeaconTower extension attributes (stored as typed objects, not strings)
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cloudEvent[CloudEventExtensions.CorrelationId] = correlationId;
        }

        if (userId.HasValue)
        {
            // CloudEvents extension attributes require string values for unregistered attributes
            cloudEvent[CloudEventExtensions.UserId] = userId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            cloudEvent[CloudEventExtensions.UserName] = userName;
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
    /// Example: "device-api" results in source "//beacontower/device-api".
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
