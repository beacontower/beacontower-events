using BeaconTower.Events.Abstractions;
using BeaconTower.Events.InMemory;
using BeaconTower.Events.Nats;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
