using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonMap
{
    public IReadOnlyList<DungeonRoom> Rooms { get; }
    public IReadOnlyList<Rectangle> Corridors { get; }
    public Rectangle WorldBounds { get; }
    public DungeonRoom StartRoom { get; }
    public DungeonRoom ExitRoom { get; }

    public DungeonMap(
        IReadOnlyList<DungeonRoom> rooms,
        IReadOnlyList<Rectangle> corridors,
        Rectangle worldBounds)
    {
        Rooms = rooms;
        Corridors = corridors;
        WorldBounds = worldBounds;

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type == RoomType.Start)
                StartRoom = room;
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

        return null;
    }
}
