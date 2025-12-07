namespace BeaconTower.Events.Nats;

/// <summary>
/// Maps CloudEvent type strings to NATS subjects.
/// </summary>
/// <remarks>
/// CloudEvent type: cloud.beacontower.device.created
/// NATS subject: beacontower.device.created
/// </remarks>
public static class CloudEventSubjectMapper
{
    private const string CloudPrefix = "cloud.";

    /// <summary>
    /// Converts a CloudEvent type to a NATS subject.
    /// </summary>
    /// <param name="cloudEventType">The CloudEvent type (e.g., "cloud.beacontower.device.created").</param>
    /// <returns>The NATS subject (e.g., "beacontower.device.created").</returns>
    public static string ToNatsSubject(string cloudEventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudEventType);

        // Strip "cloud." prefix if present
        if (cloudEventType.StartsWith(CloudPrefix, StringComparison.Ordinal))
        {
            return cloudEventType[CloudPrefix.Length..];
        }

        return cloudEventType;
    }

    /// <summary>
    /// Converts a NATS subject back to a CloudEvent type.
    /// </summary>
    /// <param name="natsSubject">The NATS subject (e.g., "beacontower.device.created").</param>
    /// <returns>The CloudEvent type (e.g., "cloud.beacontower.device.created").</returns>
    public static string ToCloudEventType(string natsSubject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(natsSubject);

        return $"{CloudPrefix}{natsSubject}";
    }
}
