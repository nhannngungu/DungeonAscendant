using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum WildForestSectionKind
{
    ForestOutskirts,
    GoblinEncampment,
    Webwood,
    Thornlands,
    CorruptedGrove,
    WarCamp,
    MothersNest,
    AncientSanctuary
}

public enum WildForestLandmarkKind
{
    FallenWatchTree,
    SpiderGroveDeadTree,
    WarCampGate,
    GiganticRootGate,
    TreantArenaHeart,
    MothersNestMaw
}

public enum EnemyTheme
{
    None,
    WildPredators,
    GoblinCamp,
    SpiderBrood,
    ThornCorruption,
    RuinOccupiers,
    TreantTerritory,
    AncientTreant
}

public enum EncounterDifficulty
{
    Low,
    Standard,
    Hard,
    Elite,
    Boss
}

public enum SpawnSocketRole
{
    GroundMelee,
    Ranged,
    Flying,
    Ambush,
    Heavy,
    Elite,
    Boss,
    Minion
}

public enum WildForestRouteFeatureKind
{
    SideBranch,
    Shortcut,
    Secret
}

public sealed class WildForestRouteFeature
{
    public string FeatureId { get; }
    public string SectionId { get; }
    public WildForestRouteFeatureKind Kind { get; }
    public Rectangle Bounds { get; }
    public Vector2 Entry { get; }
    public Vector2 Return { get; }

    public WildForestRouteFeature(
        string featureId,
        string sectionId,
        WildForestRouteFeatureKind kind,
        Rectangle bounds,
        Vector2 entry,
        Vector2 returnPosition)
    {
        FeatureId = featureId ?? string.Empty;
        SectionId = sectionId ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        Entry = entry;
        Return = returnPosition;
    }
}

public sealed class WildForestSpawnProfile
{
    private readonly EnemyType[] _allowedEnemyTypes;
    private readonly int[] _weights;

    public string ProfileId { get; }
    public string SectionId { get; }
    public IReadOnlyList<EnemyType> AllowedEnemyTypes => _allowedEnemyTypes;
    public IReadOnlyList<int> Weights => _weights;
    public int MaximumConcurrentEnemies { get; }
    public float MinimumPlayerDistance { get; }
    public bool RequiresGround { get; }
    public bool AllowsAerial { get; }
    public int MinimumHeavyGroundWidth { get; }

    public WildForestSpawnProfile(
        string profileId,
        string sectionId,
        EnemyType[] allowedEnemyTypes,
        int[] weights,
        int maximumConcurrentEnemies,
        float minimumPlayerDistance,
        bool requiresGround,
        bool allowsAerial,
        int minimumHeavyGroundWidth = 260)
    {
        ProfileId = profileId ?? string.Empty;
        SectionId = sectionId ?? string.Empty;
        _allowedEnemyTypes = allowedEnemyTypes == null
            ? Array.Empty<EnemyType>()
            : (EnemyType[])allowedEnemyTypes.Clone();
        _weights = weights == null ? Array.Empty<int>() : (int[])weights.Clone();
        MaximumConcurrentEnemies = Math.Max(1, maximumConcurrentEnemies);
        MinimumPlayerDistance = Math.Max(96f, minimumPlayerDistance);
        RequiresGround = requiresGround;
        AllowsAerial = allowsAerial;
        MinimumHeavyGroundWidth = Math.Max(160, minimumHeavyGroundWidth);
    }
}

public sealed class WildForestSection
{
    public string SectionId { get; }
    public string Name { get; }
    public WildForestSectionKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }
    public float FogDensity { get; }
    public float CorruptionLevel { get; }

    public WildForestSection(
        string sectionId,
        string name,
        WildForestSectionKind kind,
        Rectangle bounds,
        int groundY,
        float fogDensity,
        float corruptionLevel)
    {
        SectionId = sectionId ?? string.Empty;
        Name = name ?? string.Empty;
        Kind = kind;
        Bounds = bounds;
        GroundY = groundY;
        FogDensity = Math.Clamp(fogDensity, 0f, 1f);
        CorruptionLevel = Math.Clamp(corruptionLevel, 0f, 1f);
    }
}

public sealed class WildForestLandmark
{
    public string LandmarkId { get; }
    public WildForestLandmarkKind Kind { get; }
    public string SectionId { get; }
    public Vector2 Position { get; }

    public WildForestLandmark(
        string landmarkId,
        WildForestLandmarkKind kind,
        string sectionId,
        Vector2 position)
    {
        LandmarkId = landmarkId ?? string.Empty;
        Kind = kind;
        SectionId = sectionId ?? string.Empty;
        Position = position;
    }
}

/// <summary>
/// Authored encounter intent only.  It deliberately has no update or spawn
/// behavior so the future encounter director can consume it without coupling
/// distribution logic to terrain generation.
/// </summary>
public sealed class EncounterZone
{
    private readonly EnemyType[] _suggestedEnemyTypes;

    public string ZoneId { get; }
    public string SectionId { get; }
    public Rectangle ActivationBounds { get; }
    public Rectangle GroundBounds { get; }
    public EnemyTheme Theme { get; }
    public EncounterDifficulty Difficulty { get; }
    public IReadOnlyList<EnemyType> SuggestedEnemyTypes =>
        _suggestedEnemyTypes;
    public int MaxConcurrentEnemies { get; }
    public bool IsEliteZone { get; }
    public bool IsBossZone { get; }
    public bool AllowRespawn { get; }
    public bool IntroEligible { get; }

    public EncounterZone(
        string zoneId,
        string sectionId,
        Rectangle activationBounds,
        Rectangle groundBounds,
        EnemyTheme theme,
        EncounterDifficulty difficulty,
        EnemyType[] suggestedEnemyTypes,
        int maxConcurrentEnemies,
        bool isEliteZone = false,
        bool isBossZone = false,
        bool allowRespawn = false,
        bool introEligible = true)
    {
        ZoneId = zoneId ?? string.Empty;
        SectionId = sectionId ?? string.Empty;
        ActivationBounds = activationBounds;
        GroundBounds = groundBounds;
        Theme = theme;
        Difficulty = difficulty;
        _suggestedEnemyTypes = suggestedEnemyTypes == null
            ? Array.Empty<EnemyType>()
            : (EnemyType[])suggestedEnemyTypes.Clone();
        MaxConcurrentEnemies = Math.Max(0, maxConcurrentEnemies);
        IsEliteZone = isEliteZone;
        IsBossZone = isBossZone;
        AllowRespawn = allowRespawn;
        IntroEligible = introEligible;
    }
}

/// <summary>
/// A believable future spawn origin with role and ecology metadata.  Sockets
/// never instantiate enemies themselves.
/// </summary>
public sealed class SpawnSocket
{
    private readonly EnemyType[] _suggestedEnemyTypes;

    public string SocketId { get; }
    public string ZoneId { get; }
    public SpawnSocketRole Role { get; }
    public Vector2 Position { get; }
    public Rectangle PlacementBounds { get; }
    public IReadOnlyList<EnemyType> SuggestedEnemyTypes =>
        _suggestedEnemyTypes;
    public bool IntroEligible { get; }

    public SpawnSocket(
        string socketId,
        string zoneId,
        SpawnSocketRole role,
        Vector2 position,
        Rectangle placementBounds,
        EnemyType[] suggestedEnemyTypes,
        bool introEligible = true)
    {
        SocketId = socketId ?? string.Empty;
        ZoneId = zoneId ?? string.Empty;
        Role = role;
        Position = position;
        PlacementBounds = placementBounds;
        _suggestedEnemyTypes = suggestedEnemyTypes == null
            ? Array.Empty<EnemyType>()
            : (EnemyType[])suggestedEnemyTypes.Clone();
        IntroEligible = introEligible;
    }
}
