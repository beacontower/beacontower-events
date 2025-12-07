namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Generates CloudEvents type strings following BeaconTower conventions.
/// Pattern: com.beacontower.{entity}.{action}
/// </summary>
public static class CloudEventTypeGenerator
{
    /// <summary>
    /// The BeaconTower CloudEvents type prefix.
    /// </summary>
    public const string TypePrefix = "com.beacontower";

    /// <summary>
    /// Generates a CloudEvents type string for the given entity and action.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "device", "user", "alarm").</param>
    /// <param name="action">The action (e.g., "created", "updated", "deleted").</param>
    /// <returns>The CloudEvents type string (e.g., "com.beacontower.device.created").</returns>
    #pragma warning disable CA1308 // Normalize strings to uppercase - CloudEvents type convention is lowercase
    public static string Generate(string entityType, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return $"{TypePrefix}.{entityType.ToLowerInvariant()}.{action.ToLowerInvariant()}";
    }
    #pragma warning restore CA1308

    /// <summary>
    /// Generates a CloudEvents type string for an entity created event.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The CloudEvents type string (e.g., "com.beacontower.device.created").</returns>
    public static string Created(string entityType) => Generate(entityType, "created");

    /// <summary>
    /// Generates a CloudEvents type string for an entity updated event.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The CloudEvents type string (e.g., "com.beacontower.device.updated").</returns>
    public static string Updated(string entityType) => Generate(entityType, "updated");

    /// <summary>
    /// Generates a CloudEvents type string for an entity deleted event.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The CloudEvents type string (e.g., "com.beacontower.device.deleted").</returns>
    public static string Deleted(string entityType) => Generate(entityType, "deleted");

    /// <summary>
    /// Parses a CloudEvents type string to extract entity type and action.
    /// </summary>
    /// <param name="cloudEventType">The CloudEvents type string.</param>
    /// <param name="entityType">The extracted entity type.</param>
    /// <param name="action">The extracted action.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string cloudEventType, out string entityType, out string action)
    {
        entityType = string.Empty;
        action = string.Empty;

        if (string.IsNullOrWhiteSpace(cloudEventType))
        {
            return false;
        }

        var parts = cloudEventType.Split('.');
        if (parts.Length < 4 || parts[0] != "com" || parts[1] != "beacontower")
        {
            return false;
        }

        entityType = parts[2];
        action = parts[3];
        return true;
    }
}
