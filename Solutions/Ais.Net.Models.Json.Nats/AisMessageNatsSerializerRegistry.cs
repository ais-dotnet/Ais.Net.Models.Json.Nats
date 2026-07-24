// <copyright file="AisMessageNatsSerializerRegistry.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Ais.Net.Models.Json.Nats;

using System;

using Ais.Net.Models;

using NATS.Client.Core;

/// <summary>
/// An <see cref="INatsSerializerRegistry"/> that provides <see cref="AisMessageNatsSerializer"/> for
/// <see cref="AisMessageBase"/>. Assign it to <c>NatsOpts.SerializerRegistry</c> so an AIS-broadcast
/// connection uses the JSON serializer by default, without passing a serializer on every
/// publish/subscribe.
/// </summary>
/// <remarks>
/// <para>
/// Publish and subscribe using <see cref="AisMessageBase"/> as the message type: the concrete
/// message (<see cref="AisMessageType1Through3"/>, <see cref="AisMessageType5"/>, and so on) is
/// written and reconstructed polymorphically from the <c>$type</c> discriminator within the payload.
/// </para>
/// <para>
/// The registry serves <see cref="AisMessageBase"/> only. Requesting a serializer for any other type
/// — including a concrete AIS leaf type such as <see cref="AisMessageType5"/> — throws
/// <see cref="NotSupportedException"/>, because the serializer is defined solely over the base type.
/// To mix AIS and non-AIS payloads on one connection, pass a per-call serializer instead.
/// </para>
/// </remarks>
public sealed class AisMessageNatsSerializerRegistry : INatsSerializerRegistry
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly AisMessageNatsSerializerRegistry Default = new();

    /// <inheritdoc/>
    public INatsSerialize<T> GetSerializer<T>() => GetAisSerializer<T>();

    /// <inheritdoc/>
    public INatsDeserialize<T> GetDeserializer<T>() => GetAisSerializer<T>();

    private static INatsSerializer<T> GetAisSerializer<T>()
    {
        if (typeof(T) == typeof(AisMessageBase))
        {
            return (INatsSerializer<T>)(object)AisMessageNatsSerializer.Default;
        }

        throw new NotSupportedException(
            $"{nameof(AisMessageNatsSerializerRegistry)} only handles '{typeof(AisMessageBase)}'. " +
            $"Publish and subscribe using the {nameof(AisMessageBase)} type, or supply a per-call " +
            $"serializer for '{typeof(T)}'.");
    }
}
