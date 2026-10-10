using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class Projectile
{
    public ProjectileType Type { get; }
    public Vector2 SourcePosition { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public Vector2 PreviousPosition { get; internal set; }
    public Vector2 Size { get; }
    public float LifetimeRemaining { get; internal set; }
    public int Damage { get; }
    public bool Blockable { get; }
    public bool Unblockable { get; }
    public float SlowDurationSeconds { get; }
    public float SlowMovementMultiplier { get; }
    public int RoomId { get; }
    public bool IsPlayerOwned { get; }
    public float PoiseDamage { get; }
    public float Knockback { get; }
    public bool EmpoweredVisual { get; }
    public WeaponTechniqueEffect TechniqueEffect { get; }
    public int WeaponResourceGain { get; }
    public ProjectileTrajectoryType Trajectory { get; }
    public float Gravity { get; }
    public int MaximumRicochets { get; }
    public int RicochetCount { get; internal set; }
    public int RemainingPierces { get; internal set; }
    public bool PenetratesTargets { get; }
    public float ImpactRadius { get; }
    public int SplitCount { get; }
    public bool PenetratesTerrain { get; }
    public bool HasSplit { get; internal set; }
    public bool HasHitTarget { get; internal set; }
    public int TargetsHit { get; internal set; }
    public float ChargeRatio { get; }
    public float FocusRatioAtFire { get; }
    public RangerDrawState DrawState { get; }
    public int SourceAttackId { get; }
    public int TechniqueUseId { get; }
    public int SpreadArrowIndex { get; }
    public float RicochetVisualTimeRemaining { get; internal set; }
    public Vector2 LastRicochetPosition { get; internal set; }
    public float SurfaceSeparationRemaining { get; internal set; }
    public bool IsRangerProjectile { get; }
    public float PlacementDistance { get; }
    public float CursePressure { get; }
    public bool IsSpellbladeProjectile => TechniqueEffect is
        WeaponTechniqueEffect.SpellbladeRuneBrand or
        WeaponTechniqueEffect.SpellbladeRunicSpear;
    internal HashSet<Enemy> HitEnemies { get; } = new();
    internal bool HasHitBoss { get; set; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);
    public Rectangle SweptBounds
    {
        get
        {
            int halfWidth = (int)MathF.Ceiling(Size.X / 2f);
            int halfHeight = (int)MathF.Ceiling(Size.Y / 2f);
            int left = (int)MathF.Floor(MathF.Min(PreviousPosition.X, Position.X)) - halfWidth;
            int right = (int)MathF.Ceiling(MathF.Max(PreviousPosition.X, Position.X)) + halfWidth;
            int top = (int)MathF.Floor(MathF.Min(PreviousPosition.Y, Position.Y)) - halfHeight;
            int bottom = (int)MathF.Ceiling(MathF.Max(PreviousPosition.Y, Position.Y)) + halfHeight;
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }
    }

    public Projectile(
        ProjectileType type,
        Vector2 position,
        Vector2 velocity,
        Vector2 size,
        float lifetimeSeconds,
        int damage,
        bool blockable,
        bool unblockable,
        float slowDurationSeconds,
        float slowMovementMultiplier,
        int roomId,
        bool isPlayerOwned = false,
        float poiseDamage = 0f,
        float knockback = 0f,
        bool empoweredVisual = false,
        WeaponTechniqueEffect techniqueEffect = WeaponTechniqueEffect.None,
        int weaponResourceGain = 0,
        ProjectileTrajectoryType trajectory = ProjectileTrajectoryType.Straight,
        float gravity = 0f,
        int maximumRicochets = 0,
        int remainingPierces = 0,
        float impactRadius = 0f,
        int splitCount = 0,
        bool penetratesTerrain = false,
        float chargeRatio = 0f,
        float focusRatioAtFire = 0f,
        RangerDrawState drawState = RangerDrawState.Quick,
        int sourceAttackId = -1,
        int techniqueUseId = -1,
        int spreadArrowIndex = -1,
        bool isRangerProjectile = false,
        float placementDistance = 0f,
        float cursePressure = 0f)
    {
        Type = type;
        SourcePosition = position;
        Position = position;
        PreviousPosition = position;
        Velocity = velocity;
        Size = size;
        LifetimeRemaining = lifetimeSeconds;
        Damage = damage;
        Blockable = blockable && !unblockable;
        Unblockable = unblockable;
        SlowDurationSeconds = slowDurationSeconds;
        SlowMovementMultiplier = slowMovementMultiplier;
        RoomId = roomId;
        IsPlayerOwned = isPlayerOwned;
        PoiseDamage = poiseDamage;
        Knockback = knockback;
        EmpoweredVisual = empoweredVisual;
        TechniqueEffect = techniqueEffect;
        WeaponResourceGain = weaponResourceGain;
        Trajectory = trajectory;
        Gravity = MathF.Max(0f, gravity);
        MaximumRicochets = Math.Max(0, maximumRicochets);
        RemainingPierces = Math.Max(0, remainingPierces);
        PenetratesTargets = RemainingPierces > 0;
        ImpactRadius = MathF.Max(0f, impactRadius);
        SplitCount = Math.Max(0, splitCount);
        PenetratesTerrain = penetratesTerrain;
        ChargeRatio = Math.Clamp(chargeRatio, 0f, 1f);
        FocusRatioAtFire = Math.Clamp(focusRatioAtFire, 0f, 1f);
        DrawState = drawState;
        SourceAttackId = sourceAttackId;
        TechniqueUseId = techniqueUseId;
        SpreadArrowIndex = spreadArrowIndex;
        IsRangerProjectile = isRangerProjectile;
        PlacementDistance = MathF.Max(0f, placementDistance);
        CursePressure = MathF.Max(0f, cursePressure);
    }
}
