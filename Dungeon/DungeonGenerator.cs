using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonGenerator
{
    private const int MinimumRoomCount = 7;
    private const int MaximumRoomCount = 10;
    private const int GridColumns = 4;
    private const int GridRows = 3;
    private const int CellWidth = 600;
    private const int CellHeight = 500;
    private const int WorldWidth = 2600;
    private const int WorldHeight = 1800;
    private const int CorridorWidth = 64;
    private const int GenerationAttempts = 3;

    private readonly Random _random;

    public DungeonGenerator(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
    }

    public DungeonMap Generate()
    {
        for (int attempt = 0; attempt < GenerationAttempts; attempt++)
        {
            DungeonMap dungeon = GenerateRandomDungeon();

            if (Validate(dungeon))
                return dungeon;
        }

        return CreateFallbackDungeon();
    }

    public bool Validate(DungeonMap dungeon)
    {
        if (dungeon == null ||
            dungeon.Rooms.Count < MinimumRoomCount ||
            dungeon.Rooms.Count > MaximumRoomCount ||
            dungeon.StartRoom == null ||
            dungeon.TreasureRoom == null ||
            dungeon.BossRoom == null ||
            dungeon.ExitRoom == null)
        {
            return false;
        }

        if (CountRoomsOfType(dungeon.Rooms, RoomType.Start) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Treasure) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Boss) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Exit) != 1 ||
            !AreConnected(dungeon.BossRoom, dungeon.ExitRoom))
        {
            return false;
        }

        for (int first = 0; first < dungeon.Rooms.Count; first++)
        {
            Rectangle firstBounds = dungeon.Rooms[first].Bounds;
            firstBounds.Inflate(24, 24);

            if (!dungeon.WorldBounds.Contains(dungeon.Rooms[first].Bounds))
                return false;

            for (int second = first + 1; second < dungeon.Rooms.Count; second++)
            {
                if (firstBounds.Intersects(dungeon.Rooms[second].Bounds))
                    return false;
            }
        }

        var visited = new HashSet<int> { dungeon.StartRoom.Id };
        var pending = new Queue<int>();
        pending.Enqueue(dungeon.StartRoom.Id);

        while (pending.Count > 0)
        {
            int roomId = pending.Dequeue();
            DungeonRoom room = FindRoomById(dungeon.Rooms, roomId);

            if (room == null)
                return false;

            foreach (int connectedRoomId in room.Connections)
            {
                if (visited.Add(connectedRoomId))
                    pending.Enqueue(connectedRoomId);
            }
        }

        return visited.Count == dungeon.Rooms.Count;
    }

    private DungeonMap GenerateRandomDungeon()
    {
        int roomCount = _random.Next(MinimumRoomCount, MaximumRoomCount + 1);
        var availableCells = new List<int>();

        for (int index = 0; index < GridColumns * GridRows; index++)
            availableCells.Add(index);

        Shuffle(availableCells);
        var rooms = new List<DungeonRoom>(roomCount);

        for (int index = 0; index < roomCount; index++)
        {
            int cell = availableCells[index];
            int column = cell % GridColumns;
            int row = cell / GridColumns;
            int width = _random.Next(260, 421);
            int height = _random.Next(200, 341);
            int cellX = 100 + column * CellWidth;
            int cellY = 100 + row * CellHeight;
            int x = cellX + _random.Next(40, CellWidth - width - 39);
            int y = cellY + _random.Next(40, CellHeight - height - 39);

            rooms.Add(new DungeonRoom(
                index,
                new Rectangle(x, y, width, height),
                RoomType.Normal));
        }

        DungeonRoom startRoom = FindTopLeftRoom(rooms);
        var corridors = ConnectRooms(rooms, startRoom);
        AssignRoomTypes(rooms, startRoom);
        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(0, 0, WorldWidth, WorldHeight));
    }

    private void AssignRoomTypes(
        List<DungeonRoom> rooms,
        DungeonRoom startRoom)
    {
        var parents = new Dictionary<int, int>();
        var depths = new Dictionary<int, int>();
        var pending = new Queue<DungeonRoom>();
        parents[startRoom.Id] = -1;
        depths[startRoom.Id] = 0;
        pending.Enqueue(startRoom);

        while (pending.Count > 0)
        {
            DungeonRoom room = pending.Dequeue();

            foreach (int connectedId in room.Connections)
            {
                if (depths.ContainsKey(connectedId))
                    continue;

                DungeonRoom connectedRoom = FindRoomById(rooms, connectedId);
                parents[connectedId] = room.Id;
                depths[connectedId] = depths[room.Id] + 1;
                pending.Enqueue(connectedRoom);
            }
        }

        DungeonRoom exitRoom = startRoom;
        int greatestDepth = -1;
        float greatestDistanceSquared = -1f;

        foreach (DungeonRoom room in rooms)
        {
            int depth = depths[room.Id];
            float distanceSquared = Vector2.DistanceSquared(
                room.Center,
                startRoom.Center);

            if (depth > greatestDepth ||
                depth == greatestDepth && distanceSquared > greatestDistanceSquared)
            {
                exitRoom = room;
                greatestDepth = depth;
                greatestDistanceSquared = distanceSquared;
            }
        }

        DungeonRoom bossRoom = FindRoomById(rooms, parents[exitRoom.Id]);

        if (bossRoom == startRoom)
            bossRoom = FindFarthestAvailableRoom(rooms, startRoom, exitRoom, null);

        var pathToExit = new List<DungeonRoom>();
        DungeonRoom pathRoom = exitRoom;

        while (pathRoom != null)
        {
            pathToExit.Add(pathRoom);
            int parentId = parents[pathRoom.Id];
            pathRoom = parentId < 0 ? null : FindRoomById(rooms, parentId);
        }

        pathToExit.Reverse();
        DungeonRoom treasureRoom = null;
        int preferredTreasureIndex = Math.Max(1, (pathToExit.Count - 1) / 2);

        for (int offset = 0; offset < pathToExit.Count; offset++)
        {
            int index = Math.Min(
                pathToExit.Count - 1,
                preferredTreasureIndex + offset);
            DungeonRoom candidate = pathToExit[index];

            if (candidate != startRoom &&
                candidate != bossRoom &&
                candidate != exitRoom)
            {
                treasureRoom = candidate;
                break;
            }
        }

        treasureRoom ??= FindFarthestAvailableRoom(
            rooms,
            startRoom,
            exitRoom,
            bossRoom);

        foreach (DungeonRoom room in rooms)
            room.Type = RoomType.Normal;

        startRoom.Type = RoomType.Start;
        treasureRoom.Type = RoomType.Treasure;
        bossRoom.Type = RoomType.Boss;
        exitRoom.Type = RoomType.Exit;

        int enemyRoomCount = 0;

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type != RoomType.Normal)
                continue;

            if (_random.NextDouble() < 0.6)
            {
                room.Type = RoomType.Enemy;
                enemyRoomCount++;
            }
        }

        for (int index = 0; enemyRoomCount < 2 && index < rooms.Count; index++)
        {
            DungeonRoom room = rooms[index];

            if (room.Type != RoomType.Normal)
                continue;

            room.Type = RoomType.Enemy;
            enemyRoomCount++;
        }
    }

    private static DungeonRoom FindFarthestAvailableRoom(
        IReadOnlyList<DungeonRoom> rooms,
        DungeonRoom startRoom,
        DungeonRoom excludedRoom,
        DungeonRoom secondExcludedRoom)
    {
        DungeonRoom result = null;
        float farthestDistanceSquared = -1f;

        foreach (DungeonRoom room in rooms)
        {
            if (room == startRoom ||
                room == excludedRoom ||
                room == secondExcludedRoom)
            {
                continue;
            }

            float distanceSquared = Vector2.DistanceSquared(
                room.Center,
                startRoom.Center);

            if (distanceSquared > farthestDistanceSquared)
            {
                result = room;
                farthestDistanceSquared = distanceSquared;
            }
        }

        return result;
    }

    private List<Rectangle> ConnectRooms(
        List<DungeonRoom> rooms,
        DungeonRoom startRoom)
    {
        var corridors = new List<Rectangle>();
        var connected = new List<DungeonRoom> { startRoom };
        var remaining = new List<DungeonRoom>(rooms);
        remaining.Remove(startRoom);

        while (remaining.Count > 0)
        {
            DungeonRoom nextRoom = remaining[0];
            DungeonRoom nearestRoom = connected[0];
            float nearestDistanceSquared = float.MaxValue;

            foreach (DungeonRoom candidate in remaining)
            {
                foreach (DungeonRoom existing in connected)
                {
                    float distanceSquared = Vector2.DistanceSquared(
                        candidate.Center,
                        existing.Center);

                    if (distanceSquared >= nearestDistanceSquared)
                        continue;

                    nextRoom = candidate;
                    nearestRoom = existing;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            AddConnectionCorridors(
                corridors,
                nearestRoom.Center.ToPoint(),
                nextRoom.Center.ToPoint(),
                _random.Next(2) == 0);
            nearestRoom.ConnectTo(nextRoom.Id);
            nextRoom.ConnectTo(nearestRoom.Id);
            connected.Add(nextRoom);
            remaining.Remove(nextRoom);
        }

        return corridors;
    }

    private static void AddConnectionCorridors(
        List<Rectangle> corridors,
        Point start,
        Point end,
        bool horizontalFirst)
    {
        if (horizontalFirst)
        {
            corridors.Add(CreateHorizontalCorridor(start.X, end.X, start.Y));
            corridors.Add(CreateVerticalCorridor(start.Y, end.Y, end.X));
        }
        else
        {
            corridors.Add(CreateVerticalCorridor(start.Y, end.Y, start.X));
            corridors.Add(CreateHorizontalCorridor(start.X, end.X, end.Y));
        }
    }

    private static Rectangle CreateHorizontalCorridor(int startX, int endX, int y)
    {
        return new Rectangle(
            Math.Min(startX, endX) - CorridorWidth / 2,
            y - CorridorWidth / 2,
            Math.Abs(endX - startX) + CorridorWidth,
            CorridorWidth);
    }

    private static Rectangle CreateVerticalCorridor(int startY, int endY, int x)
    {
        return new Rectangle(
            x - CorridorWidth / 2,
            Math.Min(startY, endY) - CorridorWidth / 2,
            CorridorWidth,
            Math.Abs(endY - startY) + CorridorWidth);
    }

    private static DungeonRoom FindTopLeftRoom(List<DungeonRoom> rooms)
    {
        DungeonRoom result = rooms[0];
        int bestScore = result.Bounds.Center.X + result.Bounds.Center.Y;

        foreach (DungeonRoom room in rooms)
        {
            int score = room.Bounds.Center.X + room.Bounds.Center.Y;

            if (score < bestScore)
            {
                result = room;
                bestScore = score;
            }
        }

        return result;
    }

    private static DungeonRoom FindRoomById(
        IReadOnlyList<DungeonRoom> rooms,
        int roomId)
    {
        foreach (DungeonRoom room in rooms)
        {
            if (room.Id == roomId)
                return room;
        }

        return null;
    }

    private static int CountRoomsOfType(
        IReadOnlyList<DungeonRoom> rooms,
        RoomType roomType)
    {
        int count = 0;

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type == roomType)
                count++;
        }

        return count;
    }

    private static bool AreConnected(DungeonRoom first, DungeonRoom second)
    {
        foreach (int connectedId in first.Connections)
        {
            if (connectedId == second.Id)
                return true;
        }

        return false;
    }

    private void Shuffle(List<int> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = _random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static DungeonMap CreateFallbackDungeon()
    {
        var rooms = new List<DungeonRoom>
        {
            new(0, new Rectangle(140, 160, 320, 240), RoomType.Start),
            new(1, new Rectangle(700, 140, 300, 240), RoomType.Enemy),
            new(2, new Rectangle(1260, 180, 340, 260), RoomType.Treasure),
            new(3, new Rectangle(1880, 160, 340, 250), RoomType.Enemy),
            new(4, new Rectangle(1840, 760, 360, 280), RoomType.Normal),
            new(5, new Rectangle(1160, 1120, 360, 280), RoomType.Boss),
            new(6, new Rectangle(420, 1180, 360, 280), RoomType.Exit)
        };
        var corridors = new List<Rectangle>();

        for (int index = 0; index < rooms.Count - 1; index++)
        {
            DungeonRoom first = rooms[index];
            DungeonRoom second = rooms[index + 1];
            AddConnectionCorridors(
                corridors,
                first.Center.ToPoint(),
                second.Center.ToPoint(),
                horizontalFirst: index % 2 == 0);
            first.ConnectTo(second.Id);
            second.ConnectTo(first.Id);
        }

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(0, 0, WorldWidth, WorldHeight));
    }
}
