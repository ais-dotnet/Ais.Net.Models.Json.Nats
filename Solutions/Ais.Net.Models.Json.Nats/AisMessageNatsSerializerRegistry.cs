// <copyright file="AisMessageNatsSerializerRegistry.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Ais.Net.Models.Json.Nats;

using System;

using Ais.Net.Models;

using NATS.Client.Core;

/// <summary>
/// An <see cref="INatsSerializerRegistry"/> that serializes <see cref="AisMessageBase"/> (and its
/// derived message types) using <see cref="AisMessageNatsSerializer"/>. Assign it to
/// <c>NatsOpts.SerializerRegistry</c> to make an AIS-broadcast connection use the JSON serializer by
/// default, so callers need not pass a serializer on every publish/subscribe.
/// </summary>
/// <remarks>
/// This registry is intended for connections that carry only AIS messages. Requesting a serializer
/// for any type other than <see cref="AisMessageBase"/> throws <see cref="NotSupportedException"/>;
/// to mix AIS and non-AIS payloads on one connection, pass a per-call serializer instead of using
/// this registry.
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
