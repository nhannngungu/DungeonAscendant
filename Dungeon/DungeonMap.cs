using System;
using System.Collections.Generic;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonMap
{
    private readonly HashSet<string> _brokenCollapsingStructures = new();
    public IReadOnlyList<DungeonRoom> Rooms { get; }
    public IReadOnlyList<Rectangle> Corridors { get; }
    public IReadOnlyList<Platform> Platforms { get; }
    public Rectangle WorldBounds { get; }
    public DungeonRoom StartRoom { get; }
    public DungeonRoom TreasureRoom { get; }
    public DungeonRoom BossRoom { get; }
    public DungeonRoom ExitRoom { get; }
    public bool IsAuthoredWildForest { get; }
    public bool IsAncientCatacombs { get; }
    public bool IsFallenCity { get; }
    public bool IsDemonLands { get; }
    public IReadOnlyList<WildForestSection> WildForestSections { get; }
    public IReadOnlyList<EncounterZone> EncounterZones { get; }
    public IReadOnlyList<SpawnSocket> SpawnSockets { get; }
    public IReadOnlyList<WildForestLandmark> Landmarks { get; }
    public IReadOnlyList<WildForestRouteFeature> RouteFeatures { get; }
    public IReadOnlyList<WildForestSpawnProfile> SpawnProfiles { get; }
    public Vector2? AuthoredExitPosition { get; }
    public IReadOnlyList<CatacombZoneDefinition> CatacombZones { get; }
    public IReadOnlyList<CurseZoneDefinition> CurseZones { get; }
    public IReadOnlyList<SafeShrineDefinition> SafeShrines { get; }
    public IReadOnlyList<TombInteractionDefinition> Tombs { get; }
    public IReadOnlyList<FallenCityZoneDefinition> FallenCityZones { get; }
    public IReadOnlyList<CityAlarmObjectDefinition> CityAlarmObjects { get; }
    public IReadOnlyList<CollapsingStructureDefinition> CollapsingStructures { get; }
    public IReadOnlyList<CityReinforcementDefinition> CityReinforcements { get; }
    public IReadOnlyList<DemonLandsZoneDefinition> DemonLandsZones { get; }
    public IReadOnlyList<CorruptionFieldDefinition> CorruptionFields { get; }
    public IReadOnlyList<PurificationShrineDefinition> PurificationShrines { get; }
    public IReadOnlyList<CorruptionAnchorDefinition> CorruptionAnchors { get; }

    public DungeonMap(
        IReadOnlyList<DungeonRoom> rooms,
        IReadOnlyList<Rectangle> corridors,
        Rectangle worldBounds,
        bool isAuthoredWildForest = false,
        bool isAncientCatacombs = false,
        bool isFallenCity = false,
        bool isDemonLands = false,
        IReadOnlyList<WildForestSection> wildForestSections = null,
        IReadOnlyList<EncounterZone> encounterZones = null,
        IReadOnlyList<SpawnSocket> spawnSockets = null,
        IReadOnlyList<WildForestLandmark> landmarks = null,
        IReadOnlyList<WildForestRouteFeature> routeFeatures = null,
        IReadOnlyList<WildForestSpawnProfile> spawnProfiles = null,
        IReadOnlyList<CatacombZoneDefinition> catacombZones = null,
        IReadOnlyList<CurseZoneDefinition> curseZones = null,
        IReadOnlyList<SafeShrineDefinition> safeShrines = null,
        IReadOnlyList<TombInteractionDefinition> tombs = null,
        IReadOnlyList<FallenCityZoneDefinition> fallenCityZones = null,
        IReadOnlyList<CityAlarmObjectDefinition> cityAlarmObjects = null,
        IReadOnlyList<CollapsingStructureDefinition> collapsingStructures = null,
        IReadOnlyList<CityReinforcementDefinition> cityReinforcements = null,
        IReadOnlyList<DemonLandsZoneDefinition> demonLandsZones = null,
        IReadOnlyList<CorruptionFieldDefinition> corruptionFields = null,
        IReadOnlyList<PurificationShrineDefinition> purificationShrines = null,
        IReadOnlyList<CorruptionAnchorDefinition> corruptionAnchors = null,
        DungeonRoom exitRoomOverride = null,
        Vector2? authoredExitPosition = null)
    {
        Rooms = rooms;
        Corridors = corridors;
        WorldBounds = worldBounds;
        IsAuthoredWildForest = isAuthoredWildForest;
        IsAncientCatacombs = isAncientCatacombs;
        IsFallenCity = isFallenCity;
        IsDemonLands = isDemonLands;
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
        CatacombZones = new List<CatacombZoneDefinition>(catacombZones ?? Array.Empty<CatacombZoneDefinition>());
        CurseZones = new List<CurseZoneDefinition>(curseZones ?? Array.Empty<CurseZoneDefinition>());
        SafeShrines = new List<SafeShrineDefinition>(safeShrines ?? Array.Empty<SafeShrineDefinition>());
        Tombs = new List<TombInteractionDefinition>(tombs ?? Array.Empty<TombInteractionDefinition>());
        FallenCityZones = new List<FallenCityZoneDefinition>(
            fallenCityZones ?? Array.Empty<FallenCityZoneDefinition>());
        CityAlarmObjects = new List<CityAlarmObjectDefinition>(
            cityAlarmObjects ?? Array.Empty<CityAlarmObjectDefinition>());
        CollapsingStructures = new List<CollapsingStructureDefinition>(
            collapsingStructures ?? Array.Empty<CollapsingStructureDefinition>());
        CityReinforcements = new List<CityReinforcementDefinition>(
            cityReinforcements ?? Array.Empty<CityReinforcementDefinition>());
        DemonLandsZones = new List<DemonLandsZoneDefinition>(
            demonLandsZones ?? Array.Empty<DemonLandsZoneDefinition>());
        CorruptionFields = new List<CorruptionFieldDefinition>(
            corruptionFields ?? Array.Empty<CorruptionFieldDefinition>());
        PurificationShrines = new List<PurificationShrineDefinition>(
            purificationShrines ?? Array.Empty<PurificationShrineDefinition>());
        CorruptionAnchors = new List<CorruptionAnchorDefinition>(
            corruptionAnchors ?? Array.Empty<CorruptionAnchorDefinition>());
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

    public bool IsCollapsingStructureCollisionActive(
        CollapsingStructureDefinition structure) =>
        structure != null && structure.HasCollision &&
        !_brokenCollapsingStructures.Contains(structure.StructureId);

    internal void SetCollapsingStructureBroken(string structureId)
    {
        if (!string.IsNullOrEmpty(structureId))
            _brokenCollapsingStructures.Add(structureId);
    }

    internal void ResetCollapsingStructureCollision() =>
        _brokenCollapsingStructures.Clear();
}
