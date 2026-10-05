using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class Projectile
{
    public ProjectileType Type { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; }
    public Vector2 Size { get; }
    public float LifetimeRemaining { get; internal set; }
    public int Damage { get; }
    public float SlowDurationSeconds { get; }
    public float SlowMovementMultiplier { get; }
    public int RoomId { get; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);

    public Projectile(
        ProjectileType type,
        Vector2 position,
        Vector2 velocity,
        Vector2 size,
        float lifetimeSeconds,
        int damage,
        float slowDurationSeconds,
        float slowMovementMultiplier,
        int roomId)
    {
        Type = type;
        Position = position;
        Velocity = velocity;
        Size = size;
        LifetimeRemaining = lifetimeSeconds;
        Damage = damage;
        SlowDurationSeconds = slowDurationSeconds;
        SlowMovementMultiplier = slowMovementMultiplier;
        RoomId = roomId;
    }
}
