using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// BeaconTower extension attributes for CloudEvents.
/// </summary>
public static class CloudEventExtensions
{
    /// <summary>
    /// Extension attribute name for correlation ID (distributed tracing).
    /// </summary>
    public const string CorrelationId = "correlationid";

    /// <summary>
    /// Extension attribute name for user ID.
    /// </summary>
    public const string UserId = "userid";

    /// <summary>
    /// Extension attribute name for user name.
    /// </summary>
    public const string UserName = "username";

    /// <summary>
    /// Gets the correlation ID from a CloudEvent.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent.</param>
    /// <returns>The correlation ID, or null if not present.</returns>
    public static string? GetCorrelationId(this CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        return cloudEvent[CorrelationId]?.ToString();
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
            cloudEvent[CorrelationId] = correlationId;
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
        var value = cloudEvent[UserId];

        return value switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
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
            // CloudEvents extension attributes require string values for unregistered attributes
            cloudEvent[UserId] = userId.Value.ToString();
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
        return cloudEvent[UserName]?.ToString();
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
            cloudEvent[UserName] = userName;
        }
        return cloudEvent;
    }
}
