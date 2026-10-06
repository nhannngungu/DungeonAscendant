using System.Collections.Generic;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonMap
{
    public IReadOnlyList<DungeonRoom> Rooms { get; }
    public IReadOnlyList<Rectangle> Corridors { get; }
    public IReadOnlyList<Platform> Platforms { get; }
    public Rectangle WorldBounds { get; }
    public DungeonRoom StartRoom { get; }
    public DungeonRoom TreasureRoom { get; }
    public DungeonRoom BossRoom { get; }
    public DungeonRoom ExitRoom { get; }

    public DungeonMap(
        IReadOnlyList<DungeonRoom> rooms,
        IReadOnlyList<Rectangle> corridors,
        Rectangle worldBounds)
    {
        Rooms = rooms;
        Corridors = corridors;
        WorldBounds = worldBounds;
        var platforms = new List<Platform>();

        foreach (DungeonRoom room in rooms)
            platforms.AddRange(room.Platforms);

        Platforms = platforms;

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type == RoomType.Start)
                StartRoom = room;
            else if (room.Type == RoomType.Treasure)
                TreasureRoom = room;
            else if (room.Type == RoomType.Boss)
                BossRoom = room;
            else if (room.Type == RoomType.Exit)
                ExitRoom = room;
        }
    }

    public DungeonRoom FindRoomContaining(Vector2 position)
    {
        Point point = position.ToPoint();

        foreach (DungeonRoom room in Rooms)
        {
            if (room.Bounds.Contains(point))
                return room;
        }

        foreach (Rectangle corridor in Corridors)
        {
            if (!corridor.Contains(point))
                continue;

            DungeonRoom nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (DungeonRoom room in Rooms)
            {
                float distance = System.MathF.Abs(room.Center.X - position.X);

                if (distance < nearestDistance)
                {
                    nearest = room;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        return null;
    }
}
