using System;
using System.Collections.Generic;
using DungeonAscendant.World;

namespace DungeonAscendant.Dungeon;

/// <summary>Deterministic structural checks for authored Map 1.</summary>
public static class WildForestMapValidation
{
    public static void ValidateOrThrow()
    {
        DungeonMap map = new DungeonGenerator(1701).GenerateWildForest();
        Require(map.IsAuthoredWildForest,
            "Map 1 was not marked as an authored Wild Forest map.");
        Require(map.WildForestSections.Count ==
                DungeonGenerator.AuthoredWildForestSectionCount &&
            map.Rooms.Count == DungeonGenerator.AuthoredWildForestSectionCount,
            "Map 1 must contain exactly eight authored zones.");
        Require(map.WorldBounds.Width == DungeonGenerator.AuthoredWildForestWidth &&
            map.WorldBounds.Height == DungeonGenerator.RoomHeight,
            "Map 1 world dimensions changed unexpectedly.");
        Require(map.StartRoom != null && map.TreasureRoom != null &&
            map.BossRoom != null && map.ExitRoom == map.BossRoom,
            "Map 1 room contract or arena exit override is incomplete.");
        Require(map.Landmarks.Count == 6,
            "Map 1 must expose six navigation landmarks.");
        Require(map.EncounterZones.Count == 10 && map.SpawnSockets.Count >= 25,
            "Map 1 encounter metadata is incomplete.");
        Require(map.SpawnProfiles.Count == 8,
            "Map 1 must expose one ecology profile per zone.");

        for (int index = 0; index < map.WildForestSections.Count; index++)
        {
            WildForestSection section = map.WildForestSections[index];
            Require(section.Bounds == map.Rooms[index].Bounds &&
                section.GroundY == map.Rooms[index].GroundY,
                $"Section {section.SectionId} is detached from collision geometry.");
            if (index > 0)
            {
                int gap = section.Bounds.Left -
                    map.WildForestSections[index - 1].Bounds.Right;
                Require(gap == DungeonGenerator.AuthoredTransitionWidth,
                    "Authored sections are not connected by the standard transition width.");
            }
        }

        ValidateContinuousGround(map);
        ValidateRouteFeatures(map);
        ValidateSpawnProfiles(map);
        ValidateEncounterMetadata(map);
        ValidateLandmarks(map);

        for (int seed = 0; seed < 100; seed++)
        {
            DungeonMap seeded = new DungeonGenerator(seed).GenerateWildForest();
            Require(seeded.WorldBounds == map.WorldBounds &&
                seeded.Platforms.Count == map.Platforms.Count &&
                seeded.SpawnSockets.Count == map.SpawnSockets.Count,
                $"Authored Map 1 macro layout changed for seed {seed}.");
        }

        DungeonMap showcase = new DungeonGenerator(1701)
            .GenerateWildForestShowcase();
        Require(!showcase.IsAuthoredWildForest &&
            showcase.StartRoom.Bounds.Width ==
                DungeonGenerator.WildForestShowcaseRoomWidth,
            "Wild Forest Showcase was replaced by the authored normal map.");
    }

    private static void ValidateRouteFeatures(DungeonMap map)
    {
        var ids = new HashSet<string>();
        int branches = 0;
        int shortcuts = 0;
        int secrets = 0;
        foreach (WildForestRouteFeature feature in map.RouteFeatures)
        {
            Require(ids.Add(feature.FeatureId),
                $"Duplicate route feature: {feature.FeatureId}.");
            Require(map.WorldBounds.Contains(feature.Bounds),
                $"Route feature {feature.FeatureId} leaves world bounds.");
            switch (feature.Kind)
            {
                case WildForestRouteFeatureKind.SideBranch: branches++; break;
                case WildForestRouteFeatureKind.Shortcut: shortcuts++; break;
                case WildForestRouteFeatureKind.Secret: secrets++; break;
            }
        }
        Require(branches is >= 3 and <= 5,
            "Map 1 needs three to five optional side branches.");
        Require(shortcuts is >= 1 and <= 3,
            "Map 1 needs one to three shortcuts.");
        Require(secrets is >= 2 and <= 4,
            "Map 1 needs two to four secrets.");
    }

    private static void ValidateSpawnProfiles(DungeonMap map)
    {
        var sections = new HashSet<string>();
        foreach (WildForestSpawnProfile profile in map.SpawnProfiles)
        {
            Require(sections.Add(profile.SectionId),
                $"Duplicate ecology profile for {profile.SectionId}.");
            Require(profile.AllowedEnemyTypes.Count == profile.Weights.Count,
                $"Profile {profile.ProfileId} has mismatched weights.");
            int total = 0;
            foreach (int weight in profile.Weights)
            {
                Require(weight > 0,
                    $"Profile {profile.ProfileId} has a non-positive weight.");
                total += weight;
            }
            Require(profile.AllowedEnemyTypes.Count == 0 || total == 100,
                $"Profile {profile.ProfileId} weights must total 100.");
            Require(profile.MaximumConcurrentEnemies <= 5 &&
                profile.MinimumPlayerDistance >= 250f,
                $"Profile {profile.ProfileId} violates population safety caps.");
        }
    }

    private static void ValidateContinuousGround(DungeonMap map)
    {
        int? previousTop = null;
        for (int x = map.WorldBounds.Left + 4;
             x < map.WorldBounds.Right - 4;
             x += 12)
        {
            int top = int.MaxValue;
            foreach (Platform platform in map.Platforms)
            {
                if (platform.Kind is not
                        (PlatformKind.Ground or PlatformKind.Transition) ||
                    x < platform.Bounds.Left || x >= platform.Bounds.Right)
                {
                    continue;
                }
                top = Math.Min(top, platform.Bounds.Top);
            }

            Require(top != int.MaxValue,
                $"Map 1 has an unsupported traversal gap near X={x}.");
            if (previousTop.HasValue)
            {
                Require(Math.Abs(top - previousTop.Value) <= 32,
                    $"Map 1 has an unreadable traversal step near X={x}.");
            }
            previousTop = top;
        }
    }

    private static void ValidateEncounterMetadata(DungeonMap map)
    {
        var zoneIds = new HashSet<string>();
        bool elite = false;
        bool boss = false;
        var roles = new HashSet<SpawnSocketRole>();

        foreach (EncounterZone zone in map.EncounterZones)
        {
            Require(zoneIds.Add(zone.ZoneId),
                $"Duplicate encounter zone id: {zone.ZoneId}.");
            Require(map.WorldBounds.Contains(zone.ActivationBounds) &&
                zone.MaxConcurrentEnemies > 0,
                $"Encounter zone {zone.ZoneId} has invalid bounds or capacity.");
            elite |= zone.IsEliteZone;
            boss |= zone.IsBossZone;
        }

        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            Require(zoneIds.Contains(socket.ZoneId),
                $"Spawn socket {socket.SocketId} references an unknown zone.");
            Require(map.WorldBounds.Contains(socket.Position.ToPoint()),
                $"Spawn socket {socket.SocketId} is outside Map 1.");
            EncounterZone owner = null;
            foreach (EncounterZone zone in map.EncounterZones)
            {
                if (zone.ZoneId == socket.ZoneId)
                {
                    owner = zone;
                    break;
                }
            }
            Require(owner != null &&
                owner.ActivationBounds.Contains(socket.Position.ToPoint()),
                $"Spawn socket {socket.SocketId} is outside its encounter zone.");
            roles.Add(socket.Role);
        }

        Require(elite && boss,
            "Map 1 must expose both elite and boss encounter hooks.");
        foreach (SpawnSocketRole role in Enum.GetValues<SpawnSocketRole>())
        {
            Require(roles.Contains(role),
                $"Map 1 is missing the {role} future spawn role.");
        }
    }

    private static void ValidateLandmarks(DungeonMap map)
    {
        var kinds = new HashSet<WildForestLandmarkKind>();
        foreach (WildForestLandmark landmark in map.Landmarks)
        {
            Require(kinds.Add(landmark.Kind),
                $"Duplicate Map 1 landmark kind: {landmark.Kind}.");
            Require(map.WorldBounds.Contains(landmark.Position.ToPoint()),
                $"Landmark {landmark.LandmarkId} is outside Map 1.");
        }

        Require(kinds.Count == Enum.GetValues<WildForestLandmarkKind>().Length,
            "Not every required Map 1 landmark is represented.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
