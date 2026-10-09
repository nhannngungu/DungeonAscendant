using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class BreakerImpactVisual
{
    public Vector2 Position { get; }
    public float Radius { get; }
    public float Strength { get; }
    public float TimeRemaining { get; internal set; } = .42f;
    public float Progress => 1f - MathHelper.Clamp(TimeRemaining / .42f, 0f, 1f);

    public BreakerImpactVisual(Vector2 position, float radius, float strength)
    {
        Position = position;
        Radius = radius;
        Strength = MathHelper.Clamp(strength, 0f, 1f);
    }
}
