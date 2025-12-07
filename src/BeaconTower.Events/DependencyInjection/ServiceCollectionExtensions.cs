using BeaconTower.Events.Abstractions;
using BeaconTower.Events.InMemory;
using BeaconTower.Events.Nats;
using BeaconTower.Events.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BeaconTower.Events.DependencyInjection;

/// <summary>
/// Extension methods for registering CloudEvents services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the NATS CloudEvent publisher with the specified options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure publisher options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsCloudEventPublisher(
        this IServiceCollection services,
        Action<NatsCloudEventPublisherOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<NatsCloudEventPublisherOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICloudEventPublisher, NatsCloudEventPublisher>();

        return services;
    }

    /// <summary>
    /// Registers the NATS CloudEvent publisher with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsCloudEventPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NatsCloudEventPublisherOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICloudEventPublisher, NatsCloudEventPublisher>();

        return services;
    }

    /// <summary>
    /// Registers the NATS CloudEvent subscriber with the specified options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure subscriber options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsCloudEventSubscriber(
        this IServiceCollection services,
        Action<NatsCloudEventSubscriberOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<NatsCloudEventSubscriberOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICloudEventSubscriber, NatsCloudEventSubscriber>();

        return services;
    }

    /// <summary>
    /// Registers the NATS CloudEvent subscriber with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsCloudEventSubscriber(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NatsCloudEventSubscriberOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICloudEventSubscriber, NatsCloudEventSubscriber>();

        return services;
    }

    /// <summary>
    /// Registers in-memory CloudEvents publisher and subscriber for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryCloudEvents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<InMemoryCloudEventPublisher>();
        services.AddSingleton<ICloudEventPublisher>(sp => sp.GetRequiredService<InMemoryCloudEventPublisher>());

        services.AddSingleton<InMemoryCloudEventSubscriber>();
        services.AddSingleton<ICloudEventSubscriber>(sp => sp.GetRequiredService<InMemoryCloudEventSubscriber>());

        return services;
    }

    /// <summary>
    /// Registers a CloudEvent handler with scoped lifetime.
    /// </summary>
    /// <typeparam name="TData">The type of event data the handler processes.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCloudEventHandler<TData, THandler>(this IServiceCollection services)
        where THandler : class, ICloudEventHandler<TData>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICloudEventHandler<TData>, THandler>();

        return services;
    }

    /// <summary>
    /// Registers CloudEvents metrics for instrumentation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCloudEventsMetrics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CloudEventsMetrics>();

        return services;
    }

    /// <summary>
    /// Registers the NATS health check with ASP.NET Core health checks.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="connectionString">The NATS connection string.</param>
    /// <param name="name">The name of the health check (default: "nats").</param>
    /// <param name="failureStatus">The failure status (default: Unhealthy).</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="timeout">Optional timeout override.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddNatsHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = "nats",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.Configure<NatsHealthCheckOptions>(name, options =>
        {
            options.ConnectionString = connectionString;
            if (timeout.HasValue)
            {
                options.Timeout = timeout.Value;
            }
        });

        return builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var options = new NatsHealthCheckOptions { ConnectionString = connectionString };
                if (timeout.HasValue)
                {
                    options.Timeout = timeout.Value;
                }
                return new NatsHealthCheck(Microsoft.Extensions.Options.Options.Create(options));
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Registers the NATS health check using options from configuration.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configureOptions">Action to configure health check options.</param>
    /// <param name="name">The name of the health check (default: "nats").</param>
    /// <param name="failureStatus">The failure status (default: Unhealthy).</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddNatsHealthCheck(
        this IHealthChecksBuilder builder,
        Action<NatsHealthCheckOptions> configureOptions,
        string name = "nats",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new NatsHealthCheckOptions();
        configureOptions(options);

        builder.Services.Configure(name, configureOptions);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new NatsHealthCheck(Microsoft.Extensions.Options.Options.Create(options)),
            failureStatus,
            tags,
            options.Timeout));
    }
}
