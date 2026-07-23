# Ais.Net.Models.Json.Nats

A [NATS.Net](https://github.com/nats-io/nats.net) serializer for the [Ais.Net.Models](https://github.com/ais-dotnet/Ais.Net.Models) AIS message types, built on the low-allocation, polymorphic [Ais.Net.Models.Json](https://github.com/ais-dotnet/Ais.Net.Models.Json) serializer.

Publish decoded AIS messages over [NATS](https://nats.io/) as UTF-8 JSON and reconstruct the concrete .NET type on the receiving end. Sponsored by [endjin](https://endjin.com).

## Features

- Implements NATS.Net's `INatsSerializer<AisMessageBase>` (serialize + deserialize) over `Ais.Net.Models.Json`.
- **Polymorphic**: publish and subscribe as `AisMessageBase`; a `$type` discriminator reconstructs the correct concrete message (`AisMessageType1Through3`, `AisMessageType5`, …) on receipt.
- **Low allocation**: serialization writes straight to the NATS `IBufferWriter<byte>` (no intermediate array); deserialization reads the payload span directly for the common single-segment case.
- An optional `INatsSerializerRegistry` so an AIS-only connection can use the serializer by default.

## Usage

### Per publish / subscribe

```csharp
using Ais.Net.Models;
using Ais.Net.Models.Json.Nats;
using NATS.Net;

await using var nats = new NatsClient();

// Publish — any AisMessageBase is written as polymorphic UTF-8 JSON.
await nats.PublishAsync("ais.stream", message, serializer: AisMessageNatsSerializer.Default);

// Subscribe — the concrete type is reconstructed from the $type discriminator.
await foreach (var msg in nats.SubscribeAsync<AisMessageBase>(
    "ais.stream", serializer: AisMessageNatsSerializer.Default))
{
    if (msg.Data is AisMessageType18 positionReport)
    {
        // ...
    }
}
```

### Default serializer for a connection

For a connection that carries only AIS messages, register the serializer once so callers need not pass it on every operation:

```csharp
using Ais.Net.Models.Json.Nats;
using NATS.Client.Core;

var opts = new NatsOpts { SerializerRegistry = AisMessageNatsSerializerRegistry.Default };
await using var connection = new NatsConnection(opts);
```

> The registry handles `AisMessageBase` only; requesting a serializer for any other type throws `NotSupportedException`. To mix AIS and non-AIS payloads on one connection, pass a per-call serializer instead.

## Building

Requires the .NET 10 SDK. Depends on the `Ais.Net.Models.Json` (1.0.0 or later) and `NATS.Client.Core` packages.

```
dotnet build Solutions/Ais.Net.Models.Json.Nats.slnx -c Release
```

## Licenses

Licensed under the [Apache 2.0 License](./LICENSE).
