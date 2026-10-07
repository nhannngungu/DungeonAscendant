using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class ProjectileDefinition
{
    public ProjectileType Type { get; }
    public float Speed { get; }
    public Vector2 Size { get; }
    public float LifetimeSeconds { get; }

    public ProjectileDefinition(
        ProjectileType type,
        float speed,
        Vector2 size,
        float lifetimeSeconds)
    {
        Type = type;
        Speed = speed;
        Size = size;
        LifetimeSeconds = lifetimeSeconds;
    }
}
