namespace Ais.Net.Models.Json.Nats.Specs;

using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Ais.Net.Models;
using Ais.Net.Models.Abstractions;
using Ais.Net.Models.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NATS.Client.Core;

using NSubstitute;

using Shouldly;

[TestClass]
public class AisMessageNatsSerializerTests
{
    [TestMethod]
    [DataRow("t1_3")]
    [DataRow("t5")]
    [DataRow("t18")]
    [DataRow("t19")]
    [DataRow("t24p0")]
    [DataRow("t24p1")]
    [DataRow("t27")]
    public void RoundTripsThroughTheSerializer(string tag)
    {
        AisMessageBase message = TestMessages.Create(tag);

        // Serialize via the NATS serializer (writes straight to the buffer writer).
        ArrayBufferWriter<byte> writer = new();
        AisMessageNatsSerializer.Default.Serialize(writer, message);

        // Deserialize from a single-segment sequence (the common NATS payload shape).
        ReadOnlySequence<byte> payload = new(writer.WrittenMemory);
        payload.IsSingleSegment.ShouldBeTrue();

        AisMessageBase? result = AisMessageNatsSerializer.Default.Deserialize(in payload);

        result.ShouldNotBeNull();
        result.ShouldBe(message);
        result.GetType().ShouldBe(message.GetType());
    }

    [TestMethod]
    public void SerializerOutputMatchesAisMessageJson()
    {
        AisMessageBase message = TestMessages.Create("t18");

        ArrayBufferWriter<byte> writer = new();
        AisMessageNatsSerializer.Default.Serialize(writer, message);

        // The adapter must not alter the wire format produced by the underlying JSON serializer.
        writer.WrittenSpan.ToArray().ShouldBe(AisMessageJson.SerializeToUtf8Bytes(message));
    }

    [TestMethod]
    public void DeserializesFromAMultiSegmentSequence()
    {
        AisMessageBase message = TestMessages.Create("t5");
        byte[] json = AisMessageJson.SerializeToUtf8Bytes(message);

        // Split the payload across many small segments to exercise the non-contiguous path.
        ReadOnlySequence<byte> segmented = CreateMultiSegmentSequence(json, segmentSize: 8);
        segmented.IsSingleSegment.ShouldBeFalse();

        AisMessageBase? result = AisMessageNatsSerializer.Default.Deserialize(in segmented);

        result.ShouldBe(message);
        result.ShouldBeOfType<AisMessageType5>();
    }

    [TestMethod]
    public void CombineWithIgnoresTheNextSerializerAndReturnsItself()
    {
        // Pass a *distinct* serializer as 'next' so the assertion proves the serializer is terminal
        // (returns itself), not merely that it echoes whatever was passed in.
        INatsSerializer<AisMessageBase> next = Substitute.For<INatsSerializer<AisMessageBase>>();

        AisMessageNatsSerializer.Default.CombineWith(next).ShouldBeSameAs(AisMessageNatsSerializer.Default);
    }

    [TestMethod]
    public void ConcurrentRoundTripsAreThreadSafe()
    {
        // The Default instance and the underlying read-only JsonSerializerOptions are shared across
        // all NATS publishers/subscribers, which operate concurrently. Verify the stateless design
        // holds up under parallel use (no shared mutable state, no torn reads).
        AisMessageBase message = TestMessages.Create("t18");

        Parallel.For(0, 2000, _ =>
        {
            ArrayBufferWriter<byte> writer = new();
            AisMessageNatsSerializer.Default.Serialize(writer, message);
            ReadOnlySequence<byte> payload = new(writer.WrittenMemory);
            AisMessageNatsSerializer.Default.Deserialize(in payload).ShouldBe(message);
        });
    }

    [TestMethod]
    public void RegistryProvidesTheSerializerForAisMessageBase()
    {
        AisMessageNatsSerializerRegistry.Default.GetSerializer<AisMessageBase>()
            .ShouldBeSameAs(AisMessageNatsSerializer.Default);
        AisMessageNatsSerializerRegistry.Default.GetDeserializer<AisMessageBase>()
            .ShouldBeSameAs(AisMessageNatsSerializer.Default);
    }

    [TestMethod]
    public void RegistryRoundTripsAMessageObtainedThroughIt()
    {
        AisMessageBase message = TestMessages.Create("t19");

        INatsSerialize<AisMessageBase> serializer = AisMessageNatsSerializerRegistry.Default.GetSerializer<AisMessageBase>();
        INatsDeserialize<AisMessageBase> deserializer = AisMessageNatsSerializerRegistry.Default.GetDeserializer<AisMessageBase>();

        ArrayBufferWriter<byte> writer = new();
        serializer.Serialize(writer, message);
        ReadOnlySequence<byte> payload = new(writer.WrittenMemory);

        deserializer.Deserialize(in payload).ShouldBe(message);
    }

    [TestMethod]
    public void RegistryThrowsForUnsupportedTypes()
    {
        Should.Throw<NotSupportedException>(
            () => AisMessageNatsSerializerRegistry.Default.GetSerializer<string>());
        Should.Throw<NotSupportedException>(
            () => AisMessageNatsSerializerRegistry.Default.GetDeserializer<int>());
    }

    [TestMethod]
    public void RegistryThrowsForConcreteAisSubtypes()
    {
        // The registry is keyed on AisMessageBase; the polymorphic serializer only exists for the
        // base type, so requesting a concrete leaf type is not served. Publishing/subscribing must
        // use AisMessageBase. This test locks that (deliberately sharp) contract.
        Should.Throw<NotSupportedException>(
            () => AisMessageNatsSerializerRegistry.Default.GetSerializer<AisMessageType18>());
        Should.Throw<NotSupportedException>(
            () => AisMessageNatsSerializerRegistry.Default.GetDeserializer<AisMessageType1Through3>());
    }

    [TestMethod]
    public void DeserializeOfAnEmptyPayloadThrows()
    {
        // A truncated/empty NATS payload must surface as a JSON error, not a silent null.
        ReadOnlySequence<byte> empty = new(Array.Empty<byte>());

        Should.Throw<JsonException>(() => AisMessageNatsSerializer.Default.Deserialize(in empty));
    }

    [TestMethod]
    public void DeserializeOfAMalformedPayloadThrows()
    {
        // Well-formed prefix, then truncated mid-object.
        ReadOnlySequence<byte> malformed = new(Encoding.UTF8.GetBytes("{\"$type\":\"t18\",\"Mmsi\":"));

        Should.Throw<JsonException>(() => AisMessageNatsSerializer.Default.Deserialize(in malformed));
    }

    [TestMethod]
    public void DeserializeOfAMalformedMultiSegmentPayloadThrows()
    {
        // Same, but split across segments so the non-contiguous path is also exercised on bad input.
        ReadOnlySequence<byte> malformed = CreateMultiSegmentSequence(
            Encoding.UTF8.GetBytes("{\"$type\":\"t18\",\"Mmsi\":"), segmentSize: 4);
        malformed.IsSingleSegment.ShouldBeFalse();

        Should.Throw<JsonException>(() => AisMessageNatsSerializer.Default.Deserialize(in malformed));
    }

    [TestMethod]
    public void DeserializeOfTheJsonNullLiteralReturnsNull()
    {
        ReadOnlySequence<byte> nullLiteral = new(Encoding.UTF8.GetBytes("null"));

        AisMessageNatsSerializer.Default.Deserialize(in nullLiteral).ShouldBeNull();
    }

    [TestMethod]
    public void DeserializeOfAnUnknownDiscriminatorThrows()
    {
        // The polymorphism config uses UnknownDerivedTypeHandling = FailSerialization, so an
        // unrecognised $type tag must be rejected rather than silently mishandled.
        ReadOnlySequence<byte> unknownTag = new(Encoding.UTF8.GetBytes("{\"$type\":\"t99\",\"Mmsi\":12345}"));

        Should.Throw<JsonException>(() => AisMessageNatsSerializer.Default.Deserialize(in unknownTag));
    }

    private static ReadOnlySequence<byte> CreateMultiSegmentSequence(byte[] data, int segmentSize)
    {
        BufferSegment? first = null;
        BufferSegment? current = null;

        for (int offset = 0; offset < data.Length; offset += segmentSize)
        {
            int length = Math.Min(segmentSize, data.Length - offset);
            ReadOnlyMemory<byte> memory = new(data, offset, length);
            current = first is null
                ? first = new BufferSegment(memory, runningIndex: 0)
                : current!.Append(memory);
        }

        return new ReadOnlySequence<byte>(first!, 0, current!, current!.Memory.Length);
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            this.Memory = memory;
            this.RunningIndex = runningIndex;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            BufferSegment next = new(memory, this.RunningIndex + this.Memory.Length);
            this.Next = next;
            return next;
        }
    }
}
