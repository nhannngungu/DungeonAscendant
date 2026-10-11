using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum FallenCityZoneKind
{
    BrokenOuterGate,
    RefugeeStreet,
    MarketRuins,
    WatchDistrict,
    BurnedQuarter,
    NobleAvenue,
    ExecutionSquare,
    RoyalGuardBarracks,
    PalaceApproach,
    RuinedThronePlaza
}

public enum CollapsingStructureKind
{
    WeakBalcony,
    BrokenStair,
    DestructibleBarricade,
    CollapsingWall,
    CrackedBridge
}

public sealed class FallenCityZoneDefinition
{
    public string ZoneId { get; }
    public string NameKey { get; }
    public FallenCityZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }
    public bool RaisesAlarmOnEntry { get; }

    public FallenCityZoneDefinition(
        string zoneId,
        string nameKey,
        FallenCityZoneKind kind,
        Rectangle bounds,
        int groundY,
        bool raisesAlarmOnEntry = false)
    {
        ZoneId = zoneId ?? string.Empty;
        NameKey = nameKey ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        GroundY = groundY;
        RaisesAlarmOnEntry = raisesAlarmOnEntry;
    }
}

public sealed class CityAlarmObjectDefinition
{
    public string ObjectId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }
    public float DisableRadius { get; }

    public CityAlarmObjectDefinition(
        string objectId,
        string zoneId,
        Vector2 position,
        float disableRadius = 90f)
    {
        ObjectId = objectId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
        DisableRadius = MathF.Max(48f, disableRadius);
    }
}

public sealed class CollapsingStructureDefinition
{
    public string StructureId { get; }
    public string ZoneId { get; }
    public CollapsingStructureKind Kind { get; }
    public Rectangle Bounds { get; }
    public bool OpensAlternateRoute { get; }
    public bool IsMandatorySupport { get; }
    public bool HasCollision { get; }

    public CollapsingStructureDefinition(
        string structureId,
        string zoneId,
        CollapsingStructureKind kind,
        Rectangle bounds,
        bool opensAlternateRoute,
        bool isMandatorySupport = false,
        bool hasCollision = false)
    {
        StructureId = structureId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        OpensAlternateRoute = opensAlternateRoute;
        IsMandatorySupport = isMandatorySupport;
        HasCollision = hasCollision;
    }
}

public sealed class CityReinforcementDefinition
{
    public int MinimumAlarmLevel { get; }
    public SpawnSocket Socket { get; }

    public CityReinforcementDefinition(int minimumAlarmLevel, SpawnSocket socket)
    {
        MinimumAlarmLevel = Math.Clamp(minimumAlarmLevel, 1, 3);
        Socket = socket;
    }
}
