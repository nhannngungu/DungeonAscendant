using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public readonly struct ProjectileImpact
{
    public Projectile Projectile { get; }
    public Vector2 Position { get; }

    public ProjectileImpact(Projectile projectile, Vector2 position)
    {
        Projectile = projectile;
        Position = position;
    }
}
