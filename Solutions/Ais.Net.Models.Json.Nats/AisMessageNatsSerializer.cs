// <copyright file="AisMessageNatsSerializer.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Ais.Net.Models.Json.Nats;

using System.Buffers;

using Ais.Net.Models;
using Ais.Net.Models.Json;

using NATS.Client.Core;

/// <summary>
/// A NATS.Net serializer and deserializer for AIS messages that delegates to the low-allocation,
/// polymorphic <see cref="AisMessageJson"/> serializer.
/// </summary>
/// <remarks>
/// <para>
/// Serialization writes directly to the NATS <see cref="IBufferWriter{T}"/>, so no intermediate
/// byte array is allocated. Deserialization reads the payload span directly when the incoming
/// <see cref="ReadOnlySequence{T}"/> is a single segment (the common case); only a multi-segment
/// payload incurs a copy (via <c>ReadOnlySequence&lt;byte&gt;.ToArray()</c>) to present a
/// contiguous span to the JSON reader.
/// </para>
/// <para>
/// The serializer is stateless and thread-safe; use the shared <see cref="Default"/> instance.
/// Because the underlying serializer is polymorphic (a <c>$type</c> discriminator selects the
/// concrete message type), publish and subscribe using the <see cref="AisMessageBase"/> type and
/// the correct derived message is reconstructed on the receiving end.
/// </para>
/// </remarks>
public sealed class AisMessageNatsSerializer : INatsSerializer<AisMessageBase>
{
    /// <summary>
    /// Gets the shared, stateless instance.
    /// </summary>
    public static readonly AisMessageNatsSerializer Default = new();

    /// <inheritdoc/>
    public void Serialize(IBufferWriter<byte> bufferWriter, AisMessageBase value) =>
        AisMessageJson.Serialize(bufferWriter, value);

    /// <inheritdoc/>
    public AisMessageBase? Deserialize(in ReadOnlySequence<byte> buffer) =>
        buffer.IsSingleSegment
            ? AisMessageJson.Deserialize(buffer.FirstSpan)
            : AisMessageJson.Deserialize(buffer.ToArray());

    /// <inheritdoc/>
    /// <remarks>
    /// This serializer is terminal: it fully handles <see cref="AisMessageBase"/> and does not
    /// chain to a next serializer, so the current instance is returned unchanged.
    /// </remarks>
    public INatsSerializer<AisMessageBase> CombineWith(INatsSerializer<AisMessageBase> next) => this;
}
