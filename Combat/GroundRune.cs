using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class GroundRune
{
    public int Id { get; }
    public Vector2 Position { get; }
    public int RoomId { get; }
    public float LifetimeRemaining { get; internal set; }
    public GroundRuneState State { get; internal set; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(
        Position - new Vector2(0f, 4f),
        new Vector2(SpellbladeTuning.RuneWidth, 8f));

    public GroundRune(int id, Vector2 position, int roomId)
    {
        Id = id;
        Position = position;
        RoomId = roomId;
        LifetimeRemaining = SpellbladeTuning.RuneLifetimeSeconds;
        State = GroundRuneState.Active;
    }
}
