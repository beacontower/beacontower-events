using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using BeaconTower.Events.Abstractions;
using CloudNative.CloudEvents;

namespace BeaconTower.Events.InMemory;

/// <summary>
/// In-memory implementation of <see cref="ICloudEventSubscriber"/> for unit testing.
/// Supports wildcard pattern matching on CloudEvent type.
/// </summary>
public class InMemoryCloudEventSubscriber : ICloudEventSubscriber
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCloudEventSubscriber"/> class.
    /// </summary>
    public InMemoryCloudEventSubscriber()
        : this(new JsonSerializerOptions(JsonSerializerDefaults.Web))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCloudEventSubscriber"/> class.
    /// </summary>
    /// <param name="jsonOptions">JSON serializer options for deserializing event data.</param>
    public InMemoryCloudEventSubscriber(JsonSerializerOptions jsonOptions)
    {
        _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        ICloudEventHandler<TData> handler,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPattern);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(
            subjectPattern,
            ConvertPatternToRegex(subjectPattern),
            async (cloudEvent, ct) =>
            {
                var data = DeserializeData<TData>(cloudEvent);
                await handler.HandleAsync(cloudEvent, data, ct).ConfigureAwait(false);
            });

        _subscriptions.AddOrUpdate(
            subjectPattern,
            _ => [subscription],
            (_, list) => { list.Add(subscription); return list; });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        Func<CloudEvent, TData?, CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPattern);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(
            subjectPattern,
            ConvertPatternToRegex(subjectPattern),
            async (cloudEvent, ct) =>
            {
                var data = DeserializeData<TData>(cloudEvent);
                await handler(cloudEvent, data, ct).ConfigureAwait(false);
            });

        _subscriptions.AddOrUpdate(
            subjectPattern,
            _ => [subscription],
            (_, list) => { list.Add(subscription); return list; });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Delivers a CloudEvent to all matching subscriptions.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent to deliver.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the delivery operation.</returns>
    public async Task DeliverAsync(CloudEvent cloudEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);

        var type = cloudEvent.Type ?? string.Empty;

        foreach (var kvp in _subscriptions)
        {
            foreach (var subscription in kvp.Value)
            {
                if (subscription.Pattern.IsMatch(type))
                {
                    await subscription.Handler(cloudEvent, ct).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    public void ClearSubscriptions()
    {
        _subscriptions.Clear();
    }

    private TData? DeserializeData<TData>(CloudEvent cloudEvent)
    {
        var data = cloudEvent.Data;
        if (data is null)
        {
            return default;
        }

        // If data is already the correct type, return it directly
        if (data is TData typed)
        {
            return typed;
        }

        // If data is a JsonElement, deserialize it
        if (data is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<TData>(jsonElement.GetRawText(), _jsonOptions);
        }

        // If data is a string (JSON), deserialize it
        if (data is string json)
        {
            return JsonSerializer.Deserialize<TData>(json, _jsonOptions);
        }

        // Try to serialize and deserialize (fallback for other object types)
        var serialized = JsonSerializer.Serialize(data, _jsonOptions);
        return JsonSerializer.Deserialize<TData>(serialized, _jsonOptions);
    }

    /// <summary>
    /// Converts a NATS-style wildcard pattern to a regex.
    /// - * matches a single token (between dots)
    /// - > matches one or more tokens to the end
    /// </summary>
    private static Regex ConvertPatternToRegex(string pattern)
    {
        // First replace wildcards with placeholders, then escape, then restore
        var regexPattern = pattern
            .Replace("*", "\x01")           // Placeholder for *
            .Replace(">", "\x02");          // Placeholder for >

        regexPattern = Regex.Escape(regexPattern)
            .Replace("\x01", @"[^.]+")      // * matches single token (no dots)
            .Replace("\x02", @".+");        // > matches one or more characters

        return new Regex($"^{regexPattern}$", RegexOptions.Compiled);
    }

    private sealed record Subscription(
        string OriginalPattern,
        Regex Pattern,
        Func<CloudEvent, CancellationToken, Task> Handler);
}
