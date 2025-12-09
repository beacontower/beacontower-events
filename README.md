# BeaconTower.Events

CloudEvents-based event publishing and subscription using NATS JetStream.

## Installation

```bash
dotnet add package BeaconTower.Events
```

## Quick Start

### Publishing Events

```csharp
// Register publisher
services.AddNatsCloudEventPublisher(opts =>
{
    opts.ConnectionString = "nats://localhost:4222";
    opts.StreamName = "BEACONTOWER";
});

// Publish
var cloudEvent = CloudEventFactory.Create(
    type: "beacontower.device.created",
    source: new Uri("beacontower://device-api"),
    data: new DeviceCreatedEvent { DeviceId = id });

await publisher.PublishAsync(cloudEvent);
```

### Subscribing to Events

```csharp
// Register subscriber
services.AddNatsCloudEventSubscriber(opts =>
{
    opts.ConnectionString = "nats://localhost:4222";
    opts.StreamName = "BEACONTOWER";
    opts.ConsumerName = "device-processor";
});

// Subscribe
await subscriber.SubscribeAsync<DeviceCreatedEvent>(
    "beacontower.device.created",
    async (cloudEvent, data, ct) => Console.WriteLine($"Device: {data?.DeviceId}"));
```

### Testing (In-Memory)

```csharp
services.AddInMemoryCloudEvents();
```

## GitHub Packages Setup

Add to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/beacontower/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
    </github>
  </packageSourceCredentials>
  <packageSourceMapping>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    <packageSource key="github"><package pattern="BeaconTower.*" /></packageSource>
  </packageSourceMapping>
</configuration>
```

Set your GitHub token: `export GITHUB_TOKEN=ghp_your_token`

## Development

```bash
dotnet build
dotnet test
```
