using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BeaconTower.Events.Abstractions;
using BeaconTower.Events.Observability;
using BeaconTower.Observability;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace BeaconTower.Events.Nats;

/// <summary>
/// NATS JetStream-based CloudEvent subscriber with durable consumers,
/// consumer groups, and dead letter queue support.
/// </summary>
public sealed class NatsCloudEventSubscriber : ICloudEventSubscriber, IAsyncDisposable
{
    private readonly NatsConnection _connection;
    private readonly NatsJSContext _jetStream;
    private readonly NatsCloudEventSubscriberOptions _options;
    private readonly ILogger<NatsCloudEventSubscriber> _logger;
    private readonly CloudEventsMetrics? _metrics;
    private readonly JsonEventFormatter _formatter;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _subscriptions = new();
    private readonly SemaphoreSlim _concurrencySemaphore;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsCloudEventSubscriber"/> class.
    /// </summary>
    public NatsCloudEventSubscriber(
        IOptions<NatsCloudEventSubscriberOptions> options,
        ILogger<NatsCloudEventSubscriber> logger,
        CloudEventsMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _formatter = new JsonEventFormatter();
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);

        var natsOpts = new NatsOpts
        {
            Url = _options.ConnectionString
        };
        _connection = new NatsConnection(natsOpts);
        _jetStream = new NatsJSContext(_connection);
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        ICloudEventHandler<TData> handler,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPattern);
        ArgumentNullException.ThrowIfNull(handler);

        return SubscribeInternalAsync(
            subjectPattern,
            async (cloudEvent, data, token) =>
                await handler.HandleAsync(cloudEvent, (TData?)data, token).ConfigureAwait(false),
            typeof(TData),
            ct);
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TData>(
        string subjectPattern,
        Func<CloudEvent, TData?, CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPattern);
        ArgumentNullException.ThrowIfNull(handler);

        return SubscribeInternalAsync(
            subjectPattern,
            async (cloudEvent, data, token) =>
                await handler(cloudEvent, (TData?)data, token).ConfigureAwait(false),
            typeof(TData),
            ct);
    }

    private async Task SubscribeInternalAsync(
        string subjectPattern,
        Func<CloudEvent, object?, CancellationToken, Task> handler,
        Type dataType,
        CancellationToken ct)
    {
        var natsSubject = ConvertPatternToNatsSubject(subjectPattern);
        var consumerName = GenerateConsumerName(subjectPattern);

        Log.CreatingDurableConsumer(_logger, consumerName, natsSubject);

        // Create durable consumer configuration
        var consumerConfig = new ConsumerConfig(consumerName)
        {
            DurableName = consumerName,
            FilterSubject = natsSubject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            AckWait = _options.AckWait,
            MaxDeliver = _options.MaxDeliveryAttempts,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        // Get or create stream first
        INatsJSStream stream;
        try
        {
            stream = await _jetStream.GetStreamAsync(_options.StreamName, cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            Log.CreatingStream(_logger, _options.StreamName, "beacontower.>");

            var streamConfig = new StreamConfig(
                _options.StreamName,
                ["beacontower.>"])
            {
                Retention = StreamConfigRetention.Limits,
                Storage = StreamConfigStorage.File
            };

            stream = await _jetStream.CreateStreamAsync(streamConfig, ct).ConfigureAwait(false);
        }

        // Create or update the consumer
        var consumer = await stream.CreateOrUpdateConsumerAsync(consumerConfig, ct)
            .ConfigureAwait(false);

        // Create a linked CTS for subscription lifecycle
        var subscriptionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _subscriptions[subjectPattern] = subscriptionCts;

        // Start the message processing loop in the background
        _ = ProcessMessagesAsync(consumer, handler, dataType, subjectPattern, subscriptionCts.Token);
    }

    private async Task ProcessMessagesAsync(
        INatsJSConsumer consumer,
        Func<CloudEvent, object?, CancellationToken, Task> handler,
        Type dataType,
        string subjectPattern,
        CancellationToken ct)
    {
        Log.StartingProcessingLoop(_logger, subjectPattern);

        try
        {
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: ct).ConfigureAwait(false))
            {
                // Respect concurrency limit
                await _concurrencySemaphore.WaitAsync(ct).ConfigureAwait(false);

                // Process message without blocking the consume loop
                _ = ProcessSingleMessageAsync(msg, handler, dataType, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Log.ProcessingLoopCancelled(_logger, subjectPattern);
        }
        catch (NatsJSApiException ex)
        {
            Log.NatsApiError(_logger, ex, subjectPattern);
        }
        catch (NatsException ex)
        {
            Log.NatsError(_logger, ex, subjectPattern);
        }
    }

    private async Task ProcessSingleMessageAsync(
        NatsJSMsg<byte[]> msg,
        Func<CloudEvent, object?, CancellationToken, Task> handler,
        Type dataType,
        CancellationToken ct)
    {
        try
        {
            CloudEvent? cloudEvent = null;
            string? correlationId = null;
            Activity? activity = null;

            try
            {
                // Parse CloudEvent from Structured Content Mode
                var json = Encoding.UTF8.GetString(msg.Data ?? []);
                cloudEvent = _formatter.DecodeStructuredModeMessage(
                    Encoding.UTF8.GetBytes(json),
                    null,
                    BeaconTowerCloudEventExtensionAttributes.AllAttributes);

                correlationId = cloudEvent.GetCorrelationId();
                var eventId = cloudEvent.Id ?? "unknown";
                var eventType = cloudEvent.Type ?? "unknown";

                // Create Activity for distributed tracing
                activity = ActivityExtensions.StartActivity("CloudEvents.Process", ActivityKind.Consumer);
                activity?.AddTag("cloudevents.type", eventType);
                activity?.AddTag("cloudevents.id", eventId);
                activity?.AddTag("cloudevents.subject", msg.Subject);

                using var scope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["CloudEventType"] = eventType,
                    ["CloudEventId"] = eventId
                });

                Log.Processing(_logger, eventId, eventType);

                // Deserialize the data
                var data = DeserializeData(cloudEvent, dataType);

                // Execute the handler with timing
                var stopwatch = CloudEventsMetrics.StartTimer();
                await handler(cloudEvent, data, ct).ConfigureAwait(false);
                stopwatch.Stop();

                // Record success metrics and logging
                var durationMs = stopwatch.Elapsed.TotalMilliseconds;
                _metrics?.RecordProcessingDuration(eventType, stopwatch.Elapsed, success: true);
                Log.HandlerCompleted(_logger, eventId, durationMs);

                // Success - ACK the message
                await msg.AckAsync(cancellationToken: ct).ConfigureAwait(false);

                activity?.SetStatusOk();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Record exception in activity
                activity?.SetStatusError(ex.Message);
                activity?.RecordException(ex);

                // Handler failed - check if we should route to DLQ
                var metadata = msg.Metadata;
                var deliveryCount = (int)(metadata?.NumDelivered ?? 1);
                var eventId = cloudEvent?.Id ?? "unknown";
                var eventType = cloudEvent?.Type ?? "unknown";

                // Record failure metrics
                _metrics?.RecordProcessingDuration(eventType, TimeSpan.Zero, success: false);

                if (deliveryCount >= _options.MaxDeliveryAttempts)
                {
                    Log.HandlerFailedRoutingToDlq(_logger, ex, eventId, _options.MaxDeliveryAttempts);

                    await RouteToDlqAsync(msg, cloudEvent, ex, ct).ConfigureAwait(false);

                    // ACK the original message so it's not redelivered
                    await msg.AckAsync(cancellationToken: ct).ConfigureAwait(false);
                }
                else
                {
                    Log.HandlerFailedWillRetry(_logger, ex, eventId, deliveryCount, _options.MaxDeliveryAttempts);

                    // NACK to trigger redelivery
                    await msg.NakAsync(cancellationToken: ct).ConfigureAwait(false);
                }
            }
            finally
            {
                activity?.Dispose();
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task RouteToDlqAsync(
        NatsJSMsg<byte[]> msg,
        CloudEvent? cloudEvent,
        Exception exception,
        CancellationToken ct)
    {
        // Route to {subject}.dlq
        var dlqSubject = $"{msg.Subject}.dlq";
        var eventId = cloudEvent?.Id ?? "unknown";

        try
        {
            // Publish original message to DLQ with error metadata
            // We publish the raw message data to preserve it
            await _jetStream.PublishAsync(
                dlqSubject,
                msg.Data ?? [],
                cancellationToken: ct).ConfigureAwait(false);

            Log.RoutedToDlq(_logger, eventId, dlqSubject);
        }
        catch (NatsException ex)
        {
            Log.DlqRoutingFailed(_logger, ex, eventId, dlqSubject);
        }
    }

    private object? DeserializeData(CloudEvent cloudEvent, Type dataType)
    {
        var data = cloudEvent.Data;
        if (data is null)
        {
            return null;
        }

        // If data is already the correct type, return it directly
        if (dataType.IsInstanceOfType(data))
        {
            return data;
        }

        // If data is a JsonElement, deserialize it
        if (data is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), dataType, _jsonOptions);
        }

        // If data is a string (JSON), deserialize it
        if (data is string json)
        {
            return JsonSerializer.Deserialize(json, dataType, _jsonOptions);
        }

        // Try to serialize and deserialize (fallback for other object types)
        var serialized = JsonSerializer.Serialize(data, _jsonOptions);
        return JsonSerializer.Deserialize(serialized, dataType, _jsonOptions);
    }

    /// <summary>
    /// Converts a CloudEvent type pattern to a NATS subject pattern.
    /// Cloud prefix is stripped (cloud.beacontower.* -> beacontower.*)
    /// </summary>
    private static string ConvertPatternToNatsSubject(string pattern)
    {
        // If pattern starts with cloud., strip it
        const string cloudPrefix = "cloud.";
        if (pattern.StartsWith(cloudPrefix, StringComparison.Ordinal))
        {
            return pattern[cloudPrefix.Length..];
        }

        return pattern;
    }

    /// <summary>
    /// Generates a consistent consumer name from the subject pattern and service name.
    /// </summary>
    private string GenerateConsumerName(string subjectPattern)
    {
        // Replace wildcards and dots with underscores for valid consumer name
        var safeName = subjectPattern
            .Replace("*", "_star_")
            .Replace(">", "_gt_")
            .Replace(".", "_");

        var serviceName = string.IsNullOrEmpty(_options.ServiceName)
            ? "default"
            : _options.ServiceName;

        return $"{serviceName}_{safeName}";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel all subscriptions
        foreach (var cts in _subscriptions.Values)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
        }
        _subscriptions.Clear();

        _concurrencySemaphore.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
