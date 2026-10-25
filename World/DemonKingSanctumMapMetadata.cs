using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum DemonKingSanctumZoneKind
{
    SanctumApproach,
    HallOfBrokenVows,
    PilgrimsDescent,
    AltarOfSacrifice,
    HallOfAncientKings,
    AbyssalCloister,
    ThroneAscent,
    RoyalAntechamber,
    ThroneSanctum,
    FinalEndpoint
}

public enum SanctumLayer
{
    Background,
    Midground,
    Foreground
}

public enum SanctumPropKind
{
    BuriedWall,
    SealedArch,
    MonumentalColumn,
    OathTablet,
    BrokenBanner,
    RoyalStatue,
    Terrace,
    BrokenBridge,
    RitualAltar,
    RitualChannel,
    SuspendedChain,
    AncientVessel,
    KingAlcove,
    BrokenCrown,
    VoidWindow,
    FloatingStone,
    NarrowBridge,
    RoyalGate,
    ThroneSilhouette,
    Throne,
    SacredFragment,
    Brazier
}

public sealed class DemonKingSanctumZoneDefinition
{
    public string ZoneId { get; }
    public string NameKey { get; }
    public DemonKingSanctumZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }

    public DemonKingSanctumZoneDefinition(
        string zoneId,
        string nameKey,
        DemonKingSanctumZoneKind kind,
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

public sealed class SanctumPropDefinition
{
    public string PropId { get; }
    public string ZoneId { get; }
    public SanctumPropKind Kind { get; }
    public Rectangle Bounds { get; }
    public SanctumLayer Layer { get; }

    public SanctumPropDefinition(
        string propId,
        string zoneId,
        SanctumPropKind kind,
        Rectangle bounds,
        SanctumLayer layer)
    {
        PropId = propId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        Layer = layer;
    }
}

public sealed class SanctumCheckpointDefinition
{
    public string CheckpointId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }

    public SanctumCheckpointDefinition(
        string checkpointId,
        string zoneId,
        Vector2 position)
    {
        CheckpointId = checkpointId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
    }
}
