using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class Projectile
{
    public ProjectileType Type { get; }
    public Vector2 SourcePosition { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; }
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
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);

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
        bool empoweredVisual = false)
    {
        Type = type;
        SourcePosition = position;
        Position = position;
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
    }
}
