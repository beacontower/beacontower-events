using System.Collections.ObjectModel;
using CloudNative.CloudEvents;

namespace BeaconTower.Events.Abstractions;

/// <summary>
/// BeaconTower extension attributes for CloudEvents.
/// These attributes are registered with the CloudEvents SDK for proper type handling.
/// </summary>
public static class BeaconTowerCloudEventExtensionAttributes
{
    /// <summary>
    /// Extension attribute for correlation ID (distributed tracing).
    /// </summary>
    public static CloudEventAttribute CorrelationId { get; } =
        CloudEventAttribute.CreateExtension("correlationid", CloudEventAttributeType.String);

    /// <summary>
    /// Extension attribute for user ID (stored as Binary/16 bytes for UUID efficiency).
    /// </summary>
    public static CloudEventAttribute UserId { get; } =
        CloudEventAttribute.CreateExtension("userid", CloudEventAttributeType.Binary);

    /// <summary>
    /// Extension attribute for user name.
    /// </summary>
    public static CloudEventAttribute UserName { get; } =
        CloudEventAttribute.CreateExtension("username", CloudEventAttributeType.String);

    /// <summary>
    /// All BeaconTower extension attributes for easy registration.
    /// Pass this to CloudEvent constructor or protocol binding methods.
    /// </summary>
    public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
        new ReadOnlyCollection<CloudEventAttribute>(new[]
        {
            CorrelationId,
            UserId,
            UserName
        });
}
