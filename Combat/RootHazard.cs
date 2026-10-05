using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class RootHazard
{
    public Vector2 Position { get; }
    public Vector2 Size { get; }
    public int Damage { get; }
    public float SlowMultiplier { get; }
    public float SlowDurationSeconds { get; }
    public int RoomId { get; }
    public object Owner { get; }
    public bool IsTerrainRoot { get; }
    public RootHazardState State { get; internal set; }
    public float TimeRemaining { get; internal set; }
    public float ActiveDurationSeconds { get; }
    public bool HasDamagedPlayer { get; internal set; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);

    public RootHazard(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        float slowMultiplier,
        float slowDurationSeconds,
        int roomId,
        object owner,
        bool isTerrainRoot)
    {
        Position = position;
        Size = size;
        Damage = damage;
        SlowMultiplier = slowMultiplier;
        SlowDurationSeconds = slowDurationSeconds;
        RoomId = roomId;
        Owner = owner;
        IsTerrainRoot = isTerrainRoot;
        State = RootHazardState.Telegraph;
        TimeRemaining = telegraphSeconds;
        ActiveDurationSeconds = activeSeconds;
    }
}
