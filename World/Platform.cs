using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum PlatformKind
{
    Ground,
    Raised,
    Obstacle,
    Transition
}

/// <summary>
/// Immutable solid side-view geometry. Gameplay systems use the same bounds
/// for movement, projectile collision, and rendering.
/// </summary>
public sealed class Platform
{
    public Rectangle Bounds { get; }
    public PlatformKind Kind { get; }
    public int RoomId { get; }

    public Platform(Rectangle bounds, PlatformKind kind, int roomId)
    {
        Bounds = bounds;
        Kind = kind;
        RoomId = roomId;
    }
}
