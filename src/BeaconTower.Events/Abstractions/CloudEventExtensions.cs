using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Extension methods for accessing BeaconTower CloudEvent extension attributes.
/// Uses the registered <see cref="BeaconTowerCloudEventExtensionAttributes"/> for type safety.
/// </summary>
public static class CloudEventExtensions
{
    /// <summary>
    /// Gets the correlation ID from a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <returns>The correlation ID, or null if not present.</returns>
    public static string? GetCorrelationId(this CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        return cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId] as string;
    }

    /// <summary>
    /// Sets the correlation ID on a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <param name="correlationId">The correlation ID to set.</param>
    /// <returns>The CloudEvent for chaining.</returns>
    public static CloudEvent SetCorrelationId(this CloudEvent cloudEvent, string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId] = correlationId;
        }
        return cloudEvent;
    }

    /// <summary>
    /// Gets the user ID from a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <returns>The user ID as a GUID, or null if not present or invalid.</returns>
    public static Guid? GetUserId(this CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        var value = cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId];

        return value switch
        {
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => null
        };
    }

    /// <summary>
    /// Sets the user ID on a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <param name="userId">The user ID to set.</param>
    /// <returns>The CloudEvent for chaining.</returns>
    public static CloudEvent SetUserId(this CloudEvent cloudEvent, Guid? userId)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        if (userId.HasValue)
        {
            // Store as binary (16 bytes) for efficiency - no string allocation
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId] = userId.Value.ToByteArray();
        }
        return cloudEvent;
    }

    /// <summary>
    /// Gets the user name from a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <returns>The user name, or null if not present.</returns>
    public static string? GetUserName(this CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        return cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName] as string;
    }

    /// <summary>
    /// Sets the user name on a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <param name="userName">The user name to set.</param>
    /// <returns>The CloudEvent for chaining.</returns>
    public static CloudEvent SetUserName(this CloudEvent cloudEvent, string? userName)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        if (!string.IsNullOrWhiteSpace(userName))
        {
            cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName] = userName;
        }
        return cloudEvent;
    }
}
