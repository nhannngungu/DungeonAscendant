using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum DemonFortressZoneKind
{
    OuterBattlefield,
    DemonGate,
    Barracks,
    InfernalForge,
    CitadelRamparts,
    CommandCitadel,
    BallistaBastion,
    ExecutionCourt,
    SealedAscent
}

public enum FortressLayer
{
    Background,
    Midground,
    Foreground
}

public enum FortressPropKind
{
    Trench,
    BrokenSiegeEngine,
    Barricade,
    WeaponDebris,
    DamagedBanner,
    GateTower,
    Battlement,
    Chain,
    GateMechanism,
    Bunk,
    WeaponRack,
    ShieldRack,
    Crate,
    OfficerTable,
    TrainingDummy,
    Pillar,
    WarBell,
    Furnace,
    Anvil,
    SuspendedMetal,
    ForgeMachine,
    Vent,
    MoltenChannel,
    Ballista,
    Cover,
    WarTable,
    TrophyWeapons,
    CommandBanner,
    SealedGate
}

public sealed class DemonFortressZoneDefinition
{
    public string ZoneId { get; }
    public string NameKey { get; }
    public DemonFortressZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }

    public DemonFortressZoneDefinition(
        string zoneId,
        string nameKey,
        DemonFortressZoneKind kind,
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

public sealed class FortressPropDefinition
{
    public string PropId { get; }
    public string ZoneId { get; }
    public FortressPropKind Kind { get; }
    public Rectangle Bounds { get; }
    public FortressLayer Layer { get; }

    public FortressPropDefinition(
        string propId,
        string zoneId,
        FortressPropKind kind,
        Rectangle bounds,
        FortressLayer layer)
    {
        PropId = propId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        Layer = layer;
    }
}

public sealed class FortressCheckpointDefinition
{
    public string CheckpointId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }

    public FortressCheckpointDefinition(
        string checkpointId,
        string zoneId,
        Vector2 position)
    {
        CheckpointId = checkpointId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
    }
}
