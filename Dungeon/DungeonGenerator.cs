using System;
using System.Collections.Generic;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

/// <summary>
/// Builds readable left-to-right dungeons. Elevated geometry is optional;
/// the continuous ground is always a valid route through the floor.
/// </summary>
public sealed class DungeonGenerator
{
    public const int WildForestShowcaseRoomWidth = 9600;
    public const int MinimumRoomWidth = 1100;
    public const int MaximumRoomWidth = 1700;
    public const int RoomHeight = 720;
    public const int CorridorWidth = 144;
    public const int GroundThickness = 96;
    public const int MaximumPlatformRise = 108;
    public const int MinimumPlatformWidth = 190;
    public const int MaximumPlatformWidth = 320;

    private const int MinimumRoomCount = 7;
    private const int MaximumRoomCount = 10;
    private const int WorldMargin = 80;
    private const int GenerationAttempts = 8;
    private const int EntranceClearance = 190;
    private readonly Random _random;

    public DungeonGenerator(int? randomSeed = null)
    {
        _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
    }

    public DungeonMap Generate()
    {
        for (int attempt = 0; attempt < GenerationAttempts; attempt++)
        {
            DungeonMap dungeon = GenerateLinearDungeon();

            if (Validate(dungeon))
                return dungeon;
        }

        return CreateFallbackDungeon();
    }

    /// <summary>
    /// Creates a deterministic, obstacle-free room for the Wild Forest enemy
    /// showcase. The remaining utility rooms keep the normal DungeonMap
    /// contract intact, but all showcase combat happens in the first room.
    /// </summary>
    public DungeonMap GenerateWildForestShowcase()
    {
        int[] widths =
        {
            WildForestShowcaseRoomWidth,
            MinimumRoomWidth,
            MaximumRoomWidth,
            MinimumRoomWidth
        };
        RoomType[] types =
        {
            RoomType.Start,
            RoomType.Treasure,
            RoomType.Boss,
            RoomType.Exit
        };
        var rooms = new List<DungeonRoom>(widths.Length);
        var corridors = new List<Rectangle>(widths.Length - 1);
        int x = WorldMargin;

        for (int index = 0; index < widths.Length; index++)
        {
            var room = new DungeonRoom(
                index,
                new Rectangle(x, WorldMargin, widths[index], RoomHeight),
                types[index]);
            room.GroundY = room.Bounds.Bottom - GroundThickness;
            room.AddPlatform(new Platform(
                new Rectangle(
                    room.Bounds.Left,
                    room.GroundY,
                    room.Bounds.Width,
                    GroundThickness),
                PlatformKind.Ground,
                room.Id));
            rooms.Add(room);

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(
                    previous.Bounds.Right,
                    WorldMargin,
                    CorridorWidth,
                    RoomHeight);
                corridors.Add(corridor);
                previous.AddPlatform(new Platform(
                    new Rectangle(
                        corridor.Left,
                        previous.GroundY,
                        corridor.Width,
                        GroundThickness),
                    PlatformKind.Transition,
                    previous.Id));
            }

            x += widths[index] + CorridorWidth;
        }

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(
                rooms[0].Bounds.Left,
                rooms[0].Bounds.Top,
                rooms[^1].Bounds.Right - rooms[0].Bounds.Left,
                RoomHeight));
    }

    public bool Validate(DungeonMap dungeon)
    {
        if (dungeon == null || dungeon.Rooms.Count < MinimumRoomCount ||
            dungeon.Rooms.Count > MaximumRoomCount || dungeon.StartRoom == null ||
            dungeon.TreasureRoom == null || dungeon.BossRoom == null ||
            dungeon.ExitRoom == null ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Start) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Treasure) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Boss) != 1 ||
            CountRoomsOfType(dungeon.Rooms, RoomType.Exit) != 1)
        {
            return false;
        }

        for (int index = 0; index < dungeon.Rooms.Count; index++)
        {
            DungeonRoom room = dungeon.Rooms[index];

            if (room.Bounds.Width < MinimumRoomWidth ||
                room.Bounds.Width > MaximumRoomWidth ||
                room.Bounds.Height != RoomHeight ||
                !dungeon.WorldBounds.Contains(room.Bounds) ||
                !HasSafeGround(room) || !HasValidPlatforms(room))
            {
                return false;
            }

            if (index > 0 && !AreConnected(room, dungeon.Rooms[index - 1]))
                return false;
        }

        return AreConnected(dungeon.BossRoom, dungeon.ExitRoom) &&
            dungeon.Corridors.Count == dungeon.Rooms.Count - 1;
    }

    private DungeonMap GenerateLinearDungeon()
    {
        int roomCount = _random.Next(MinimumRoomCount, MaximumRoomCount + 1);
        var rooms = new List<DungeonRoom>(roomCount);
        var corridors = new List<Rectangle>(roomCount - 1);
        int x = WorldMargin;

        for (int index = 0; index < roomCount; index++)
        {
            int width = _random.Next(MinimumRoomWidth, MaximumRoomWidth + 1);
            var room = new DungeonRoom(
                index,
                new Rectangle(x, WorldMargin, width, RoomHeight),
                RoomType.Normal);
            room.GroundY = room.Bounds.Bottom - GroundThickness;
            AddRoomGeometry(room, index > 0);
            rooms.Add(room);

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(
                    previous.Bounds.Right,
                    WorldMargin,
                    CorridorWidth,
                    RoomHeight);
                corridors.Add(corridor);
                previous.AddPlatform(new Platform(
                    new Rectangle(corridor.Left, previous.GroundY, corridor.Width, GroundThickness),
                    PlatformKind.Transition,
                    previous.Id));
            }

            x += width + CorridorWidth;
        }

        AssignRoomTypes(rooms);

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type == RoomType.Start || room.Type == RoomType.Treasure ||
                room.Type == RoomType.Boss || room.Type == RoomType.Exit)
            {
                room.ClearOptionalPlatforms();
            }
            else if (room.Type == RoomType.Enemy)
            {
                room.ClearObstacles();
            }
        }

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(
                rooms[0].Bounds.Left,
                rooms[0].Bounds.Top,
                rooms[^1].Bounds.Right - rooms[0].Bounds.Left,
                RoomHeight));
    }

    private void AddRoomGeometry(DungeonRoom room, bool allowObstacle)
    {
        room.AddPlatform(new Platform(
            new Rectangle(room.Bounds.Left, room.GroundY, room.Bounds.Width, GroundThickness),
            PlatformKind.Ground,
            room.Id));
        int platformCount = _random.Next(0, 4);
        var occupied = new List<Rectangle>();

        for (int index = 0; index < platformCount; index++)
        {
            int width = _random.Next(MinimumPlatformWidth, MaximumPlatformWidth + 1);
            int leftLimit = room.Bounds.Left + EntranceClearance;
            int rightLimit = room.Bounds.Right - EntranceClearance - width;

            if (rightLimit <= leftLimit)
                continue;

            var bounds = new Rectangle(
                _random.Next(leftLimit, rightLimit + 1),
                room.GroundY - MaximumPlatformRise,
                width,
                16);

            if (OverlapsAny(bounds, occupied, 48))
                continue;

            occupied.Add(bounds);
            room.AddPlatform(new Platform(bounds, PlatformKind.Raised, room.Id));
        }

        if (allowObstacle && _random.NextDouble() < 0.55)
        {
            int width = _random.Next(34, 61);
            int height = _random.Next(28, 49);
            int leftLimit = room.Bounds.Left + EntranceClearance + 40;
            int rightLimit = room.Bounds.Right - EntranceClearance - width - 40;

            if (rightLimit > leftLimit)
            {
                room.AddPlatform(new Platform(
                    new Rectangle(
                        _random.Next(leftLimit, rightLimit + 1),
                        room.GroundY - height,
                        width,
                        height),
                    PlatformKind.Obstacle,
                    room.Id));
            }
        }
    }

    private void AssignRoomTypes(List<DungeonRoom> rooms)
    {
        rooms[0].Type = RoomType.Start;
        rooms[^1].Type = RoomType.Exit;
        rooms[^2].Type = RoomType.Boss;
        int treasureIndex = Math.Clamp(rooms.Count / 2, 2, rooms.Count - 3);
        rooms[treasureIndex].Type = RoomType.Treasure;
        int enemyCount = 0;

        for (int index = 1; index < rooms.Count - 2; index++)
        {
            if (index == treasureIndex)
                continue;

            if (_random.NextDouble() < 0.7)
            {
                rooms[index].Type = RoomType.Enemy;
                enemyCount++;
            }
        }

        for (int index = 1; enemyCount < 2 && index < rooms.Count - 2; index++)
        {
            if (rooms[index].Type != RoomType.Normal)
                continue;

            rooms[index].Type = RoomType.Enemy;
            enemyCount++;
        }
    }

    private static bool HasSafeGround(DungeonRoom room)
    {
        foreach (Platform platform in room.Platforms)
        {
            if (platform.Kind == PlatformKind.Ground &&
                platform.Bounds.Left <= room.Bounds.Left &&
                platform.Bounds.Right >= room.Bounds.Right &&
                platform.Bounds.Top == room.GroundY)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasValidPlatforms(DungeonRoom room)
    {
        for (int first = 0; first < room.Platforms.Count; first++)
        {
            Platform platform = room.Platforms[first];

            if (!room.Bounds.Contains(platform.Bounds) &&
                platform.Kind != PlatformKind.Transition)
            {
                return false;
            }

            if (platform.Kind == PlatformKind.Raised &&
                room.GroundY - platform.Bounds.Top > MaximumPlatformRise)
            {
                return false;
            }

            for (int second = first + 1; second < room.Platforms.Count; second++)
            {
                Platform other = room.Platforms[second];

                if (platform.Kind != PlatformKind.Ground &&
                    other.Kind != PlatformKind.Ground &&
                    platform.Bounds.Intersects(other.Bounds))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool OverlapsAny(Rectangle candidate, IReadOnlyList<Rectangle> occupied, int padding)
    {
        candidate.Inflate(padding, padding);

        foreach (Rectangle bounds in occupied)
        {
            if (candidate.Intersects(bounds))
                return true;
        }

        return false;
    }

    private static int CountRoomsOfType(IReadOnlyList<DungeonRoom> rooms, RoomType type)
    {
        int count = 0;

        foreach (DungeonRoom room in rooms)
        {
            if (room.Type == type)
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

    private static DungeonMap CreateFallbackDungeon()
    {
        var rooms = new List<DungeonRoom>(MinimumRoomCount);
        var corridors = new List<Rectangle>(MinimumRoomCount - 1);
        int x = WorldMargin;

        for (int index = 0; index < MinimumRoomCount; index++)
        {
            RoomType type = index switch
            {
                0 => RoomType.Start,
                3 => RoomType.Treasure,
                5 => RoomType.Boss,
                6 => RoomType.Exit,
                _ => RoomType.Enemy
            };
            var room = new DungeonRoom(index, new Rectangle(x, WorldMargin, 1200, RoomHeight), type);
            room.GroundY = room.Bounds.Bottom - GroundThickness;
            room.AddPlatform(new Platform(
                new Rectangle(room.Bounds.Left, room.GroundY, room.Bounds.Width, GroundThickness),
                PlatformKind.Ground,
                room.Id));
            rooms.Add(room);

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(previous.Bounds.Right, WorldMargin, CorridorWidth, RoomHeight);
                corridors.Add(corridor);
                previous.AddPlatform(new Platform(
                    new Rectangle(corridor.Left, previous.GroundY, corridor.Width, GroundThickness),
                    PlatformKind.Transition,
                    previous.Id));
            }

            x += 1200 + CorridorWidth;
        }

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(
                rooms[0].Bounds.Left,
                rooms[0].Bounds.Top,
                rooms[^1].Bounds.Right - rooms[0].Bounds.Left,
                RoomHeight));
    }
}
