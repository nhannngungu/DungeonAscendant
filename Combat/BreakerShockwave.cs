using System.Collections.Generic;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class BreakerShockwave
{
    public Vector2 Position { get; internal set; }
    public float Direction { get; }
    public float DistanceTraveled { get; internal set; }
    public float MaximumDistance { get; }
    public float Speed { get; }
    public int NearDamage { get; }
    public int FarDamage { get; }
    public float NearPoise { get; }
    public float FarPoise { get; }
    public bool IsExpired { get; internal set; }
    public HashSet<Enemy> HitEnemies { get; } = new();
    public bool HitBoss { get; internal set; }
    public float Progress => MaximumDistance <= 0f
        ? 1f
        : MathHelper.Clamp(DistanceTraveled / MaximumDistance, 0f, 1f);
    public Rectangle Bounds => new(
        (int)(Position.X - BreakerTuning.CataclysmWaveWidth / 2f),
        (int)(Position.Y - BreakerTuning.CataclysmWaveHeight),
        (int)BreakerTuning.CataclysmWaveWidth,
        (int)BreakerTuning.CataclysmWaveHeight);

    public BreakerShockwave(
        Vector2 position,
        float direction,
        float strengthRatio)
    {
        Position = position;
        Direction = direction < 0f ? -1f : 1f;
        MaximumDistance = BreakerTuning.CataclysmWaveRange;
        Speed = BreakerTuning.CataclysmWaveSpeed;
        strengthRatio = MathHelper.Clamp(strengthRatio, 0f, 1f);
        NearDamage = (int)MathHelper.Lerp(
            BreakerTuning.CataclysmFarDamage,
            BreakerTuning.CataclysmNearDamage,
            .55f + strengthRatio * .45f);
        FarDamage = BreakerTuning.CataclysmFarDamage;
        NearPoise = MathHelper.Lerp(
            BreakerTuning.CataclysmFarPoise,
            BreakerTuning.CataclysmNearPoise,
            .55f + strengthRatio * .45f);
        FarPoise = BreakerTuning.CataclysmFarPoise;
    }
}
