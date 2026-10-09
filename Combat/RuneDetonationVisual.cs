using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class RuneDetonationVisual
{
    public Vector2 Position { get; }
    public float TimeRemaining { get; internal set; } = .32f;
    public float Progress => 1f - MathHelper.Clamp(TimeRemaining / .32f, 0f, 1f);

    public RuneDetonationVisual(Vector2 position)
    {
        Position = position;
    }
}
