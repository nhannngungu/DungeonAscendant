using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum DemonLandsZoneKind
{
    BlackenedFrontier,
    AshWastes,
    FleshRavine,
    RiftFields,
    BoneForest,
    InfernalQuarry,
    BloodShrine,
    LegionCamp,
    GateOfHorns,
    InfernalHeart
}

public sealed class DemonLandsZoneDefinition
{
    public string ZoneId { get; }
    public string NameKey { get; }
    public DemonLandsZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }

    public DemonLandsZoneDefinition(
        string zoneId,
        string nameKey,
        DemonLandsZoneKind kind,
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

public sealed class CorruptionFieldDefinition
{
    public string FieldId { get; }
    public string ZoneId { get; }
    public Rectangle Bounds { get; }
    public float GainPerSecond { get; }
    public string AnchorId { get; }
    public bool IsRift { get; }

    public CorruptionFieldDefinition(
        string fieldId,
        string zoneId,
        Rectangle bounds,
        float gainPerSecond,
        string anchorId = null,
        bool isRift = false)
    {
        FieldId = fieldId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Bounds = bounds;
        GainPerSecond = MathF.Max(0f, gainPerSecond);
        AnchorId = anchorId ?? string.Empty;
        IsRift = isRift;
    }
}

public sealed class PurificationShrineDefinition
{
    public string ShrineId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }
    public float Radius { get; }

    public PurificationShrineDefinition(
        string shrineId,
        string zoneId,
        Vector2 position,
        float radius = 110f)
    {
        ShrineId = shrineId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
        Radius = MathF.Max(64f, radius);
    }
}

public sealed class CorruptionAnchorDefinition
{
    public string AnchorId { get; }
    public string ZoneId { get; }
    public Vector2 Position { get; }
    public float InfluenceRadius { get; }
    public int MaxHealth { get; }
    public bool EnablesReinforcement { get; }
    public bool IsBossAnchor { get; }

    public CorruptionAnchorDefinition(
        string anchorId,
        string zoneId,
        Vector2 position,
        float influenceRadius = 330f,
        int maxHealth = 180,
        bool enablesReinforcement = false,
        bool isBossAnchor = false)
    {
        AnchorId = anchorId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Position = position;
        InfluenceRadius = MathF.Max(120f, influenceRadius);
        MaxHealth = Math.Max(1, maxHealth);
        EnablesReinforcement = enablesReinforcement;
        IsBossAnchor = isBossAnchor;
    }
}
