# Ais.Net.Models.Json.Nats

A [NATS.Net](https://github.com/nats-io/nats.net) serializer for the [Ais.Net.Models](https://github.com/ais-dotnet/Ais.Net.Models) AIS message types, built on the low-allocation, polymorphic [Ais.Net.Models.Json](https://github.com/ais-dotnet/Ais.Net.Models.Json) serializer.

Publish decoded AIS messages over [NATS](https://nats.io/) as UTF-8 JSON and reconstruct the concrete .NET type on the receiving end. Sponsored by [endjin](https://endjin.com).

| Package                                                                               | Status                                                                                                                                              |
|---------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| [Ais.Net](https://github.com/ais-dotnet/Ais.Net)                                      | [![#](https://img.shields.io/nuget/v/Ais.Net.svg)](https://www.nuget.org/packages/Ais.Net/)                                                         |
| [Ais.Net.Models](https://github.com/ais-dotnet/Ais.Net.Models)                        | [![#](https://img.shields.io/nuget/v/Ais.Net.Models.svg)](https://www.nuget.org/packages/Ais.Net.Models/)                                           |
| [Ais.Net.Models.Json](https://github.com/ais-dotnet/Ais.Net.Models.Json)              | [![#](https://img.shields.io/nuget/v/Ais.Net.Models.Json.svg)](https://www.nuget.org/packages/Ais.Net.Models.Json/)                                 |
| [Ais.Net.Models.Json.Nats](https://github.com/ais-dotnet/Ais.Net.Models.Json.Nats)    | [![#](https://img.shields.io/nuget/v/Ais.Net.Models.Json.Nats.svg)](https://www.nuget.org/packages/Ais.Net.Models.Json.Nats/)                       |
| [Ais.Net.Receiver](https://github.com/ais-dotnet/Ais.Net.Receiver)                    | [![#](https://img.shields.io/nuget/v/Ais.Net.Receiver.svg)](https://www.nuget.org/packages/Ais.Net.Receiver/)                                       |
| [Ais.Net.Receiver.Storage.Azure.Blob](https://github.com/ais-dotnet/Ais.Net.Receiver) | [![#](https://img.shields.io/nuget/v/Ais.Net.Receiver.Storage.Azure.Blob.svg)](https://www.nuget.org/packages/Ais.Net.Receiver.Storage.Azure.Blob/) |

The [AIS.NET](https://github.com/ais-dotnet/) project contains a series of layers, from a [low-level zero allocation, high performance NMEA AIS sentence decoder](https://github.com/ais-dotnet/Ais.Net/), to [rich high-level C# 9.0 models](https://github.com/ais-dotnet/Ais.Net.Models/) of AIS message types, and a [receiver component](https://github.com/ais-dotnet/Ais.Net.Receiver/) that can listen to TCP streams of NMEA sentences and expose them as an `IObservable<string>` of raw sentences or a decoded `IObservable<IAisMessage>`. [`Ais.Net.Models.Json`](https://github.com/ais-dotnet/Ais.Net.Models.Json/) sits on top of `Ais.Net.Models` and turns the decoded messages into UTF-8 JSON and back, and `Ais.Net.Models.Json.Nats` is the top layer, adapting that serializer to NATS.Net so messages can be published and subscribed over NATS.

![https://github.com/ais-dotnet](https://endjincdn.blob.core.windows.net/assets/ais-dotnet-project-layers-incl-json.png)

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
using NATS.Client.Core;

await using var connection = new NatsConnection();

// Publish — any AisMessageBase is written as polymorphic UTF-8 JSON.
await connection.PublishAsync("ais.stream", message, serializer: AisMessageNatsSerializer.Default);

// Subscribe — the concrete type is reconstructed from the $type discriminator.
await foreach (var msg in connection.SubscribeAsync<AisMessageBase>(
    "ais.stream", serializer: AisMessageNatsSerializer.Default))
{
    if (msg.Data is AisMessageType18 positionReport)
    {
        // ...
    }
}
```

> Examples use `NatsConnection` from `NATS.Client.Core` (this package's dependency). The higher-level `NatsClient` from the `NATS.Net` meta-package exposes the same `PublishAsync`/`SubscribeAsync` if you prefer it.

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


[![OpenChain](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net.Models.Json.Nats/rule/66efac1a-662c-40cf-b4ec-8b34c29e9fd7?cache=false)](https://imm.endjin.com/api/imm/github/ais-dotnet/Ais.Net.Models.Json.Nats/rule/66efac1a-662c-40cf-b4ec-8b34c29e9fd7?cache=false)

