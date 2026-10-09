using System;
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
    public bool IsAuthoredWildForest { get; }
    public IReadOnlyList<WildForestSection> WildForestSections { get; }
    public IReadOnlyList<EncounterZone> EncounterZones { get; }
    public IReadOnlyList<SpawnSocket> SpawnSockets { get; }
    public IReadOnlyList<WildForestLandmark> Landmarks { get; }
    public IReadOnlyList<WildForestRouteFeature> RouteFeatures { get; }
    public IReadOnlyList<WildForestSpawnProfile> SpawnProfiles { get; }
    public Vector2? AuthoredExitPosition { get; }

    public DungeonMap(
        IReadOnlyList<DungeonRoom> rooms,
        IReadOnlyList<Rectangle> corridors,
        Rectangle worldBounds,
        bool isAuthoredWildForest = false,
        IReadOnlyList<WildForestSection> wildForestSections = null,
        IReadOnlyList<EncounterZone> encounterZones = null,
        IReadOnlyList<SpawnSocket> spawnSockets = null,
        IReadOnlyList<WildForestLandmark> landmarks = null,
        IReadOnlyList<WildForestRouteFeature> routeFeatures = null,
        IReadOnlyList<WildForestSpawnProfile> spawnProfiles = null,
        DungeonRoom exitRoomOverride = null,
        Vector2? authoredExitPosition = null)
    {
        Rooms = rooms;
        Corridors = corridors;
        WorldBounds = worldBounds;
        IsAuthoredWildForest = isAuthoredWildForest;
        WildForestSections = new List<WildForestSection>(
            wildForestSections ?? Array.Empty<WildForestSection>());
        EncounterZones = new List<EncounterZone>(
            encounterZones ?? Array.Empty<EncounterZone>());
        SpawnSockets = new List<SpawnSocket>(
            spawnSockets ?? Array.Empty<SpawnSocket>());
        Landmarks = new List<WildForestLandmark>(
            landmarks ?? Array.Empty<WildForestLandmark>());
        RouteFeatures = new List<WildForestRouteFeature>(
            routeFeatures ?? Array.Empty<WildForestRouteFeature>());
        SpawnProfiles = new List<WildForestSpawnProfile>(
            spawnProfiles ?? Array.Empty<WildForestSpawnProfile>());
        AuthoredExitPosition = authoredExitPosition;
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

        if (exitRoomOverride != null)
            ExitRoom = exitRoomOverride;
    }

    public WildForestSection FindWildForestSection(Vector2 position)
    {
        Point point = position.ToPoint();
        foreach (WildForestSection section in WildForestSections)
        {
            if (section.Bounds.Contains(point))
                return section;
        }
        return null;
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
