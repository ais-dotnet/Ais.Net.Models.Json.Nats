namespace Ais.Net.Models.Json.Nats.Specs;

using Ais.Net.Models;
using Ais.Net.Models.Abstractions;

/// <summary>
/// Shared, fully-populated sample messages for one instance of each of the seven AIS message types,
/// used by both the unit tests and the container integration test.
/// </summary>
internal static class TestMessages
{
    /// <summary>Gets the discriminator tags for all seven message types.</summary>
    public static string[] AllTags { get; } =
        ["t1_3", "t5", "t18", "t19", "t24p0", "t24p1", "t27"];

    /// <summary>Gets one populated instance of each of the seven message types.</summary>
    public static AisMessageBase[] All => [.. System.Linq.Enumerable.Select(AllTags, Create)];

    /// <summary>Creates a populated message for the given discriminator tag.</summary>
    public static AisMessageBase Create(string tag) => tag switch
    {
        "t1_3" => CreateType1Through3(messageType: 1),
        "t5" => CreateType5(),
        "t18" => CreateType18(),
        "t19" => CreateType19(),
        "t24p0" => CreateType24Part0(),
        "t24p1" => CreateType24Part1(),
        "t27" => CreateType27(),
        _ => throw new System.ArgumentOutOfRangeException(nameof(tag), tag, "Unknown message tag."),
    };

    public static AisMessageType1Through3 CreateType1Through3(int messageType) => new(
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

    public static AisMessageType5 CreateType5() => new(
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

    public static AisMessageType18 CreateType18() => new(
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

    public static AisMessageType19 CreateType19() => new(
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

    public static AisMessageType24Part0 CreateType24Part0() => new(
        Mmsi: 12345,
        PartNumber: 0,
        RepeatIndicator: 3,
        Spare160: 1);

    public static AisMessageType24Part1 CreateType24Part1() => new(
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

    public static AisMessageType27 CreateType27() => new(
        CourseOverGround: 123.45f,
        GnssPositionStatus: true,
        Mmsi: 12345,
        NavigationStatus: NavigationStatus.UnderwayUsingEngine,
        Position: new Position(1.0, 2.0),
        PositionAccuracy: true,
        RaimFlag: true,
        RepeatIndicator: 3,
        SpeedOverGround: 12.34f);
}
