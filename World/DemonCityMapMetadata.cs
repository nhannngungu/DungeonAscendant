using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum DemonCityZoneKind
{
    OuterApproach,
    CityEntrance,
    LowerDistrict,
    MarketDepths,
    IndustrialRitualDistrict,
    HighCity,
    EliteDistrict,
    PalaceApproach,
    MainBossArena,
    MapSevenExit
}

public enum DemonCityLayer
{
    Background,
    Midground,
    Foreground
}

public enum DemonCityPropKind
{
    Tower,
    BuildingFacade,
    Roofline,
    Arch,
    Bridge,
    Balcony,
    WindowStack,
    MarketStall,
    Furnace,
    Pipe,
    Chain,
    RitualCircle,
    Statue,
    Pillar,
    StreetLamp,
    Banner,
    Rail,
    Debris,
    SealedGate
}

public enum DemonCityEventSpaceKind
{
    StreetEncounter,
    PlazaEncounter,
    RitualChamber,
    VerticalEncounter,
    ElevatedBridge,
    EliteCourtyard,
    PalaceHall,
    BossGate,
    BossArena
}

public sealed class DemonCityZoneDefinition
{
    public string ZoneId { get; }
    public string NameKey { get; }
    public DemonCityZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }

    public DemonCityZoneDefinition(
        string zoneId,
        string nameKey,
        DemonCityZoneKind kind,
        Rectangle bounds,
        int groundY)
    {
        ZoneId = zoneId ?? string.Empty;
        NameKey = nameKey ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        GroundY = groundY;
    }
}

public sealed class DemonCityPropDefinition
{
    public string PropId { get; }
    public string ZoneId { get; }
    public DemonCityPropKind Kind { get; }
    public Rectangle Bounds { get; }
    public DemonCityLayer Layer { get; }

    public DemonCityPropDefinition(
        string propId,
        string zoneId,
        DemonCityPropKind kind,
        Rectangle bounds,
        DemonCityLayer layer)
    {
        PropId = propId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        Layer = layer;
    }
}

public sealed class DemonCityCheckpointDefinition
{
    public string CheckpointId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }

    public DemonCityCheckpointDefinition(
        string checkpointId,
        string zoneId,
        Vector2 position)
    {
        CheckpointId = checkpointId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
    }
}

public sealed class DemonCityEventSpaceDefinition
{
    public string EventId { get; }
    public string ZoneId { get; }
    public DemonCityEventSpaceKind Kind { get; }
    public Rectangle Bounds { get; }

    public DemonCityEventSpaceDefinition(
        string eventId,
        string zoneId,
        DemonCityEventSpaceKind kind,
        Rectangle bounds)
    {
        EventId = eventId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
    }
}
