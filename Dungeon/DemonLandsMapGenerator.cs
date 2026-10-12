using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DemonLandsMapGenerator
{
    public const int ZoneCount = 10;
    public const int WorldWidth = 13516;
    private const int Margin = 80;
    private const int TransitionWidth = 64;

    public DungeonMap Generate()
    {
        int[] widths = { 1080, 1180, 1220, 1180, 1250, 1300, 1280, 1400, 1350, 1700 };
        int[] ground = { 680, 684, 676, 688, 680, 672, 684, 676, 684, 680 };
        string[] keys =
        {
            "demon.zone.blackened_frontier", "demon.zone.ash_wastes",
            "demon.zone.flesh_ravine", "demon.zone.rift_fields",
            "demon.zone.bone_forest", "demon.zone.infernal_quarry",
            "demon.zone.blood_shrine", "demon.zone.legion_camp",
            "demon.zone.gate_of_horns", "demon.zone.infernal_heart"
        };
        DemonLandsZoneKind[] kinds =
            (DemonLandsZoneKind[])Enum.GetValues(typeof(DemonLandsZoneKind));
        var rooms = new List<DungeonRoom>(ZoneCount);
        var corridors = new List<Rectangle>(ZoneCount - 1);
        var zones = new List<DemonLandsZoneDefinition>(ZoneCount);
        int x = Margin;

        for (int index = 0; index < ZoneCount; index++)
        {
            RoomType type = index == 0 ? RoomType.Start :
                index == 6 ? RoomType.Treasure :
                index == ZoneCount - 1 ? RoomType.Boss : RoomType.Enemy;
            var room = new DungeonRoom(
                index,
                new Rectangle(x, Margin, widths[index], DungeonGenerator.RoomHeight),
                type)
            {
                GroundY = ground[index]
            };
            AddGeometry(room, kinds[index]);
            rooms.Add(room);
            zones.Add(new DemonLandsZoneDefinition(
                $"demon-{index + 1:00}", keys[index], kinds[index],
                room.Bounds, ground[index]));

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(
                    previous.Bounds.Right,
                    Margin,
                    TransitionWidth,
                    DungeonGenerator.RoomHeight);
                corridors.Add(corridor);
                AddTransition(previous, corridor, previous.GroundY, room.GroundY);
            }
            x += widths[index] + (index < ZoneCount - 1 ? TransitionWidth : 0);
        }

        var encounters = new List<EncounterZone>();
        var sockets = new List<SpawnSocket>();
        AddEncounters(zones, encounters, sockets);
        DungeonRoom arena = rooms[^1];

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(Margin, Margin, WorldWidth, DungeonGenerator.RoomHeight),
            isDemonLands: true,
            encounterZones: encounters,
            spawnSockets: sockets,
            routeFeatures: CreateRouteFeatures(zones),
            spawnProfiles: CreateProfiles(zones),
            demonLandsZones: zones,
            corruptionFields: CreateCorruptionFields(zones),
            purificationShrines: CreateShrines(zones),
            corruptionAnchors: CreateAnchors(zones),
            exitRoomOverride: arena,
            authoredExitPosition: new Vector2(arena.Bounds.Right - 120, arena.GroundY - 28));
    }

    private static void AddGeometry(DungeonRoom room, DemonLandsZoneKind kind)
    {
        int left = room.Bounds.Left;
        int width = room.Bounds.Width;
        int ground = room.GroundY;
        Ground(room, left, width, ground);

        switch (kind)
        {
            case DemonLandsZoneKind.BlackenedFrontier:
                Raised(room, left + 250, ground - 52, 190);
                Raised(room, left + 610, ground - 88, 220);
                break;
            case DemonLandsZoneKind.AshWastes:
                Raised(room, left + 360, ground - 62, 240);
                Raised(room, left + 790, ground - 96, 210);
                break;
            case DemonLandsZoneKind.FleshRavine:
                Raised(room, left + 205, ground - 55, 190);
                Raised(room, left + 480, ground - 110, 230);
                Raised(room, left + 820, ground - 64, 230);
                break;
            case DemonLandsZoneKind.RiftFields:
                Raised(room, left + 240, ground - 76, 210);
                Raised(room, left + 650, ground - 126, 250);
                break;
            case DemonLandsZoneKind.BoneForest:
                Raised(room, left + 180, ground - 52, 180);
                Raised(room, left + 450, ground - 102, 220);
                Raised(room, left + 790, ground - 148, 250);
                break;
            case DemonLandsZoneKind.InfernalQuarry:
                Raised(room, left + 300, ground - 54, 230);
                Raised(room, left + 670, ground - 108, 280);
                break;
            case DemonLandsZoneKind.BloodShrine:
                Raised(room, left + 430, ground - 72, 360);
                break;
            case DemonLandsZoneKind.LegionCamp:
                Raised(room, left + 230, ground - 70, 240);
                Raised(room, left + 820, ground - 112, 280);
                break;
            case DemonLandsZoneKind.GateOfHorns:
                Raised(room, left + 180, ground - 48, 190);
                Raised(room, left + 460, ground - 96, 220);
                Raised(room, left + 770, ground - 144, 240);
                break;
            case DemonLandsZoneKind.InfernalHeart:
                Raised(room, left + 220, ground - 38, 200);
                Raised(room, left + width - 440, ground - 38, 200);
                break;
        }
    }

    private static void AddEncounters(
        IReadOnlyList<DemonLandsZoneDefinition> zones,
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets)
    {
        Add(encounters, sockets, zones[0], "blackened-raiders", 130, 780,
            EnemyTheme.DemonFrontier, EncounterDifficulty.Low, 3,
            (SpawnSocketRole.GroundMelee, 460, -1, EnemyType.ImpRaider),
            (SpawnSocketRole.Ranged, 720, -88, EnemyType.HellArcher),
            (SpawnSocketRole.GroundMelee, 880, -1, EnemyType.ImpRaider));
        Add(encounters, sockets, zones[1], "ash-hunt", 120, 900,
            EnemyTheme.DemonWastes, EncounterDifficulty.Standard, 3,
            (SpawnSocketRole.GroundMelee, 410, -1, EnemyType.SpineHound),
            (SpawnSocketRole.GroundMelee, 670, -1, EnemyType.ImpRaider),
            (SpawnSocketRole.Ranged, 870, -96, EnemyType.HellArcher));
        Add(encounters, sockets, zones[2], "ravine-ambush", 120, 960,
            EnemyTheme.DemonRift, EncounterDifficulty.Hard, 3,
            (SpawnSocketRole.Ambush, 390, -1, EnemyType.RiftStalker),
            (SpawnSocketRole.GroundMelee, 690, -1, EnemyType.ImpRaider),
            (SpawnSocketRole.Ambush, 960, -64, EnemyType.RiftStalker));
        Add(encounters, sockets, zones[3], "rift-warden", 120, 920,
            EnemyTheme.DemonRift, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Ambush, 340, -76, EnemyType.RiftStalker),
            (SpawnSocketRole.Elite, 690, -1, EnemyType.RiftWarden),
            (SpawnSocketRole.Ranged, 900, -1, EnemyType.CorruptionPriest));
        Add(encounters, sockets, zones[4], "bone-harvest", 120, 1010,
            EnemyTheme.DemonBone, EncounterDifficulty.Hard, 4,
            (SpawnSocketRole.GroundMelee, 330, -1, EnemyType.SpineHound),
            (SpawnSocketRole.Heavy, 650, -1, EnemyType.BoneHarvester),
            (SpawnSocketRole.GroundMelee, 980, -1, EnemyType.SpineHound));
        Add(encounters, sockets, zones[5], "quarry-brute", 130, 1040,
            EnemyTheme.DemonQuarry, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Heavy, 440, -54, EnemyType.Hellbrute),
            (SpawnSocketRole.GroundMelee, 740, -1, EnemyType.ImpRaider),
            (SpawnSocketRole.Heavy, 1020, -1, EnemyType.BoneHarvester));
        Add(encounters, sockets, zones[6], "blood-ritual", 120, 1020,
            EnemyTheme.DemonShrine, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Ranged, 420, -72, EnemyType.CorruptionPriest),
            (SpawnSocketRole.Heavy, 720, -72, EnemyType.BoneHarvester),
            (SpawnSocketRole.Ranged, 980, -1, EnemyType.CorruptionPriest));
        Add(encounters, sockets, zones[7], "legion-formation", 140, 1120,
            EnemyTheme.DemonLegion, EncounterDifficulty.Elite, 4,
            (SpawnSocketRole.Elite, 390, -70, EnemyType.DemonLegionnaire),
            (SpawnSocketRole.Ranged, 650, -1, EnemyType.HellArcher),
            (SpawnSocketRole.Ranged, 880, -112, EnemyType.CorruptionPriest),
            (SpawnSocketRole.Elite, 1140, -1, EnemyType.DemonLegionnaire));
        Add(encounters, sockets, zones[8], "horn-gate", 120, 1110,
            EnemyTheme.DemonLegion, EncounterDifficulty.Elite, 4,
            (SpawnSocketRole.Heavy, 275, -48, EnemyType.Hellbrute),
            (SpawnSocketRole.Elite, 620, -96, EnemyType.RiftWarden),
            (SpawnSocketRole.Elite, 900, -144, EnemyType.DemonLegionnaire));
        Add(encounters, sockets, zones[9], "horned-general", 120, 1460,
            EnemyTheme.HornedGeneral, EncounterDifficulty.Boss, 1,
            (SpawnSocketRole.Boss, 880, -1, EnemyType.HornedGeneral));
    }

    private static void Add(
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets,
        DemonLandsZoneDefinition zone,
        string id,
        int offsetX,
        int width,
        EnemyTheme theme,
        EncounterDifficulty difficulty,
        int cap,
        params (SpawnSocketRole role, int x, int y, EnemyType type)[] data)
    {
        var activation = new Rectangle(
            zone.Bounds.Left + offsetX,
            zone.Bounds.Top + 90,
            width,
            zone.Bounds.Height - 160);
        encounters.Add(new EncounterZone(
            id, zone.ZoneId, activation,
            new Rectangle(activation.Left, zone.GroundY - 100, activation.Width, 100),
            theme, difficulty, Array.ConvertAll(data, item => item.type), cap,
            difficulty == EncounterDifficulty.Elite,
            difficulty == EncounterDifficulty.Boss,
            false, true));
        int socketIndex = 0;
        foreach (var item in data)
        {
            float y = item.y < 0 ? zone.GroundY + item.y : zone.Bounds.Top + item.y;
            var position = new Vector2(zone.Bounds.Left + item.x, y);
            sockets.Add(new SpawnSocket(
                $"{id}-{++socketIndex:00}", id, item.role, position,
                new Rectangle((int)position.X - 90, (int)position.Y - 120, 180, 150),
                new[] { item.type }, true));
        }
    }

    private static List<WildForestSpawnProfile> CreateProfiles(
        IReadOnlyList<DemonLandsZoneDefinition> zones)
    {
        EnemyType[][] roster =
        {
            new[] { EnemyType.ImpRaider, EnemyType.HellArcher },
            new[] { EnemyType.ImpRaider, EnemyType.HellArcher, EnemyType.SpineHound },
            new[] { EnemyType.ImpRaider, EnemyType.RiftStalker },
            new[] { EnemyType.RiftStalker, EnemyType.RiftWarden, EnemyType.CorruptionPriest },
            new[] { EnemyType.SpineHound, EnemyType.BoneHarvester },
            new[] { EnemyType.Hellbrute, EnemyType.BoneHarvester },
            new[] { EnemyType.CorruptionPriest, EnemyType.BoneHarvester },
            new[] { EnemyType.DemonLegionnaire, EnemyType.HellArcher, EnemyType.CorruptionPriest },
            new[] { EnemyType.Hellbrute, EnemyType.RiftWarden, EnemyType.DemonLegionnaire },
            new[] { EnemyType.HornedGeneral }
        };
        var result = new List<WildForestSpawnProfile>(ZoneCount);
        for (int index = 0; index < ZoneCount; index++)
        {
            int[] weights = new int[roster[index].Length];
            for (int item = 0; item < weights.Length; item++)
                weights[item] = 100 / weights.Length;
            weights[0] += 100 - Sum(weights);
            result.Add(new WildForestSpawnProfile(
                $"{zones[index].ZoneId}-profile", zones[index].ZoneId,
                roster[index], weights, index == 9 ? 1 : 4, 260f,
                true, false, index >= 5 ? 420 : 260));
        }
        return result;
    }

    private static List<WildForestRouteFeature> CreateRouteFeatures(
        IReadOnlyList<DemonLandsZoneDefinition> zones) => new()
    {
        Feature("ash-burrow", zones[1], WildForestRouteFeatureKind.Secret, 310, -120, 260, 110),
        Feature("ravine-ribs", zones[2], WildForestRouteFeatureKind.SideBranch, 440, -170, 500, 160),
        Feature("rift-step", zones[3], WildForestRouteFeatureKind.Shortcut, 620, -165, 300, 150),
        Feature("bone-canopy", zones[4], WildForestRouteFeatureKind.SideBranch, 420, -190, 620, 180),
        Feature("quarry-vent", zones[5], WildForestRouteFeatureKind.Secret, 650, -145, 310, 130),
        Feature("shrine-crypt", zones[6], WildForestRouteFeatureKind.Secret, 450, -125, 340, 115),
        Feature("camp-rampart", zones[7], WildForestRouteFeatureKind.Shortcut, 800, -155, 340, 145),
        Feature("horn-parapet", zones[8], WildForestRouteFeatureKind.SideBranch, 450, -190, 600, 180)
    };

    private static WildForestRouteFeature Feature(
        string id,
        DemonLandsZoneDefinition zone,
        WildForestRouteFeatureKind kind,
        int x,
        int y,
        int width,
        int height)
    {
        var bounds = new Rectangle(zone.Bounds.Left + x, zone.GroundY + y, width, height);
        return new WildForestRouteFeature(
            id, zone.ZoneId, kind, bounds,
            new Vector2(bounds.Left, bounds.Bottom),
            new Vector2(bounds.Right, bounds.Bottom));
    }

    private static List<CorruptionAnchorDefinition> CreateAnchors(
        IReadOnlyList<DemonLandsZoneDefinition> zones) => new()
    {
        Anchor("frontier-anchor", zones[0], 820),
        Anchor("rift-anchor", zones[3], 820, reinforcement: true),
        Anchor("bone-anchor", zones[4], 970),
        Anchor("quarry-anchor", zones[5], 1060, reinforcement: true),
        Anchor("shrine-anchor", zones[6], 650),
        Anchor("camp-anchor", zones[7], 1090, reinforcement: true),
        Anchor("gate-anchor", zones[8], 1030),
        Anchor("heart-anchor", zones[9], 1190, isBoss: true)
    };

    private static CorruptionAnchorDefinition Anchor(
        string id,
        DemonLandsZoneDefinition zone,
        int x,
        bool reinforcement = false,
        bool isBoss = false) => new(
            id, zone.ZoneId,
            new Vector2(zone.Bounds.Left + x, zone.GroundY),
            isBoss ? 420f : 330f,
            isBoss ? 300 : 180,
            reinforcement,
            isBoss);

    private static List<CorruptionFieldDefinition> CreateCorruptionFields(
        IReadOnlyList<DemonLandsZoneDefinition> zones) => new()
    {
        Field("frontier-scorch", zones[0], 690, 270, 6f, "frontier-anchor"),
        Field("ash-pool", zones[1], 460, 240, 4f),
        Field("ravine-flesh", zones[2], 510, 300, 7f),
        Field("rift-one", zones[3], 260, 210, 10f, "rift-anchor", true),
        Field("rift-two", zones[3], 760, 260, 11f, "rift-anchor", true),
        Field("bone-taint", zones[4], 780, 260, 7f, "bone-anchor"),
        Field("quarry-lava", zones[5], 730, 250, 8f, "quarry-anchor"),
        Field("blood-circle", zones[6], 420, 450, 12f, "shrine-anchor"),
        Field("camp-portal", zones[7], 920, 260, 9f, "camp-anchor", true),
        Field("gate-rift", zones[8], 800, 300, 12f, "gate-anchor", true),
        Field("heart-overrun", zones[9], 700, 620, 14f, "heart-anchor", true)
    };

    private static CorruptionFieldDefinition Field(
        string id,
        DemonLandsZoneDefinition zone,
        int x,
        int width,
        float gain,
        string anchor = null,
        bool rift = false) => new(
            id, zone.ZoneId,
            // Grounded Player centers sit 28 px above the support surface.
            // Keep the authored hazard band tall enough to include that point
            // so the visible corruption and gameplay trigger cannot diverge.
            new Rectangle(zone.Bounds.Left + x, zone.GroundY - 34, width, 40),
            gain, anchor, rift);

    private static List<PurificationShrineDefinition> CreateShrines(
        IReadOnlyList<DemonLandsZoneDefinition> zones) => new()
    {
        new("frontier-purifier", zones[0].ZoneId,
            new Vector2(zones[0].Bounds.Left + 210, zones[0].GroundY)),
        new("bone-purifier", zones[4].ZoneId,
            new Vector2(zones[4].Bounds.Left + 170, zones[4].GroundY)),
        new("blood-purifier", zones[6].ZoneId,
            new Vector2(zones[6].Bounds.Right - 170, zones[6].GroundY)),
        new("gate-purifier", zones[8].ZoneId,
            new Vector2(zones[8].Bounds.Left + 170, zones[8].GroundY))
    };

    private static void Ground(DungeonRoom room, int x, int width, int y) =>
        room.AddPlatform(new Platform(
            new Rectangle(x, y, width, room.Bounds.Bottom - y),
            PlatformKind.Ground,
            room.Id));

    private static void Raised(DungeonRoom room, int x, int y, int width) =>
        room.AddPlatform(new Platform(
            new Rectangle(x, y, width, 16),
            PlatformKind.Raised,
            room.Id));

    private static void AddTransition(
        DungeonRoom room,
        Rectangle corridor,
        int fromGround,
        int toGround)
    {
        int half = corridor.Width / 2;
        int first = (int)MathF.Round(MathHelper.Lerp(fromGround, toGround, .33f));
        int second = (int)MathF.Round(MathHelper.Lerp(fromGround, toGround, .67f));
        Ground(room, corridor.Left, half, first);
        Ground(room, corridor.Left + half, corridor.Width - half, second);
    }

    private static int Sum(int[] values)
    {
        int result = 0;
        foreach (int value in values)
            result += value;
        return result;
    }
}
