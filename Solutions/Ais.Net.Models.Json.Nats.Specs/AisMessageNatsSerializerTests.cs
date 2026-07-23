namespace Ais.Net.Models.Json.Nats.Specs;

using System;
using System.Buffers;

using Ais.Net.Models;
using Ais.Net.Models.Abstractions;
using Ais.Net.Models.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NATS.Client.Core;

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
        AisMessageBase message = Create(tag);

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
        AisMessageBase message = Create("t18");

        ArrayBufferWriter<byte> writer = new();
        AisMessageNatsSerializer.Default.Serialize(writer, message);

        // The adapter must not alter the wire format produced by the underlying JSON serializer.
        writer.WrittenSpan.ToArray().ShouldBe(AisMessageJson.SerializeToUtf8Bytes(message));
    }

    [TestMethod]
    public void DeserializesFromAMultiSegmentSequence()
    {
        AisMessageBase message = Create("t5");
        byte[] json = AisMessageJson.SerializeToUtf8Bytes(message);

        // Split the payload across many small segments to exercise the non-contiguous path.
        ReadOnlySequence<byte> segmented = CreateMultiSegmentSequence(json, segmentSize: 8);
        segmented.IsSingleSegment.ShouldBeFalse();

        AisMessageBase? result = AisMessageNatsSerializer.Default.Deserialize(in segmented);

        result.ShouldBe(message);
        result.ShouldBeOfType<AisMessageType5>();
    }

    [TestMethod]
    public void CombineWithReturnsTheSameSerializer()
    {
        INatsSerializer<AisMessageBase> other = AisMessageNatsSerializer.Default;

        AisMessageNatsSerializer.Default.CombineWith(other).ShouldBeSameAs(AisMessageNatsSerializer.Default);
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
        AisMessageBase message = Create("t19");

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

    private static AisMessageBase Create(string tag) => tag switch
    {
        "t1_3" => CreateType1Through3(messageType: 1),
        "t5" => CreateType5(),
        "t18" => CreateType18(),
        "t19" => CreateType19(),
        "t24p0" => CreateType24Part0(),
        "t24p1" => CreateType24Part1(),
        _ => CreateType27(),
    };

    private static AisMessageType1Through3 CreateType1Through3(int messageType) => new(
        CourseOverGround: 123.45f,
        ManoeuvreIndicator: ManoeuvreIndicator.NotAvailable,
        MessageType: messageType,
        Mmsi: 12345,
        NavigationStatus: NavigationStatus.UnderwayUsingEngine,
        Position: new Position(1.0, 2.0),
        PositionAccuracy: true,
        RadioSlotTimeout: 1,
        RadioSubMessage: 2,
        RadioSyncState: RadioSyncState.UtcDirect,
        RateOfTurn: 1,
        RaimFlag: true,
        RepeatIndicator: 3,
        SpareBits145: 4,
        SpeedOverGround: 12.34f,
        TimeStampSecond: 56,
        TrueHeadingDegrees: 78);

    private static AisMessageType5 CreateType5() => new(
        AisVersion: 1,
        CallSign: "CALL",
        Destination: "DEST",
        DimensionToBow: 1,
        DimensionToPort: 2,
        DimensionToStarboard: 3,
        DimensionToStern: 4,
        Draught10thMetres: 5,
        EtaDay: 6,
        EtaHour: 7,
        EtaMinute: 8,
        EtaMonth: 9,
        IsDteNotReady: true,
        ImoNumber: 123,
        Mmsi: 12345,
        PositionFixType: EpfdFixType.Gps,
        RepeatIndicator: 3,
        ShipType: (ShipType)60,
        Spare423: 1,
        VesselName: "VESSEL");

    private static AisMessageType18 CreateType18() => new(
        CanAcceptMessage22ChannelAssignment: true,
        CanSwitchBands: true,
        CourseOverGround: 123.45f,
        CsUnit: ClassBUnit.Cstdma,
        HasDisplay: true,
        IsAssigned: true,
        IsDscAttached: true,
        Mmsi: 12345,
        Position: new Position(1.0, 2.0),
        PositionAccuracy: true,
        RadioStatusType: ClassBRadioStatusType.Itdma,
        RaimFlag: true,
        RegionalReserved139: 1,
        RegionalReserved38: 2,
        RepeatIndicator: 3,
        SpeedOverGround: 12.34f,
        TimeStampSecond: 56,
        TrueHeadingDegrees: 78);

    private static AisMessageType19 CreateType19() => new(
        CourseOverGround: 123.45f,
        DimensionToBow: 1,
        DimensionToPort: 2,
        DimensionToStarboard: 3,
        DimensionToStern: 4,
        IsAssigned: true,
        IsDteNotReady: true,
        Mmsi: 12345,
        Position: new Position(1.0, 2.0),
        PositionAccuracy: true,
        PositionFixType: EpfdFixType.Gps,
        RaimFlag: true,
        RegionalReserved139: 1,
        RegionalReserved38: 2,
        RepeatIndicator: 3,
        ShipName: "SHIP",
        ShipType: (ShipType)60,
        Spare308: 1,
        SpeedOverGround: 12.34f,
        TimeStampSecond: 56,
        TrueHeadingDegrees: 78);

    private static AisMessageType24Part0 CreateType24Part0() => new(
        Mmsi: 12345,
        PartNumber: 0,
        RepeatIndicator: 3,
        Spare160: 1);

    private static AisMessageType24Part1 CreateType24Part1() => new(
        CallSign: "CALL",
        DimensionToBow: 1,
        DimensionToPort: 2,
        DimensionToStarboard: 3,
        DimensionToStern: 4,
        Mmsi: 12345,
        MothershipMmsi: 54321,
        PartNumber: 1,
        RepeatIndicator: 3,
        SerialNumber: 123,
        Spare162: 1,
        ShipType: (ShipType)60,
        UnitModelCode: 1,
        VendorIdRev3: "VEN",
        VendorIdRev4: "DOR");

    private static AisMessageType27 CreateType27() => new(
        CourseOverGround: 123.45f,
        GnssPositionStatus: true,
        Mmsi: 12345,
        NavigationStatus: NavigationStatus.UnderwayUsingEngine,
        Position: new Position(1.0, 2.0),
        PositionAccuracy: true,
        RaimFlag: true,
        RepeatIndicator: 3,
        SpeedOverGround: 12.34f);

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
