using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class WebPatch
{
    public Vector2 Position { get; }
    public Vector2 Size { get; } = new(76f, 18f);
    public float LifetimeRemaining { get; internal set; }
    public int RoomId { get; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);

    public WebPatch(Vector2 position, float lifetimeSeconds, int roomId)
    {
        Position = position;
        LifetimeRemaining = lifetimeSeconds;
        RoomId = roomId;
    }
}
