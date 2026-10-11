using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class FallenCityMapGenerator
{
    public const int ZoneCount = 10;
    public const int WorldWidth = 13726;
    private const int Margin = 80;
    private const int TransitionWidth = 64;

    public DungeonMap Generate()
    {
        int[] widths = { 1100, 1250, 1300, 1150, 1250, 1350, 1250, 1400, 1350, 1750 };
        int[] ground = { 680, 688, 680, 672, 688, 680, 684, 676, 684, 680 };
        string[] keys =
        {
            "city.zone.broken_outer_gate", "city.zone.refugee_street",
            "city.zone.market_ruins", "city.zone.watch_district",
            "city.zone.burned_quarter", "city.zone.noble_avenue",
            "city.zone.execution_square", "city.zone.royal_guard_barracks",
            "city.zone.palace_approach", "city.zone.ruined_throne_plaza"
        };
        FallenCityZoneKind[] kinds =
            (FallenCityZoneKind[])Enum.GetValues(typeof(FallenCityZoneKind));
        var rooms = new List<DungeonRoom>(ZoneCount);
        var corridors = new List<Rectangle>(ZoneCount - 1);
        var zones = new List<FallenCityZoneDefinition>(ZoneCount);
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
            zones.Add(new FallenCityZoneDefinition(
                $"city-{index + 1:00}", keys[index], kinds[index],
                room.Bounds, ground[index], index is 3 or 7 or 8));

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
        List<WildForestSpawnProfile> profiles = CreateProfiles(zones);
        List<WildForestRouteFeature> routes = CreateRouteFeatures(zones);
        List<CityAlarmObjectDefinition> alarms = CreateAlarmObjects(zones);
        List<CollapsingStructureDefinition> structures = CreateStructures(zones);
        List<CityReinforcementDefinition> reinforcements =
            CreateReinforcements(zones);
        DungeonRoom arena = rooms[^1];

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(Margin, Margin, WorldWidth, DungeonGenerator.RoomHeight),
            isFallenCity: true,
            encounterZones: encounters,
            spawnSockets: sockets,
            routeFeatures: routes,
            spawnProfiles: profiles,
            fallenCityZones: zones,
            cityAlarmObjects: alarms,
            collapsingStructures: structures,
            cityReinforcements: reinforcements,
            exitRoomOverride: arena,
            authoredExitPosition: new Vector2(arena.Bounds.Right - 120, arena.GroundY - 28));
    }

    private static void AddGeometry(DungeonRoom room, FallenCityZoneKind kind)
    {
        int left = room.Bounds.Left;
        int width = room.Bounds.Width;
        int ground = room.GroundY;
        Ground(room, left, width, ground);

        switch (kind)
        {
            case FallenCityZoneKind.BrokenOuterGate:
                Raised(room, left + 230, ground - 48, 180);
                Raised(room, left + 465, ground - 92, 210);
                break;
            case FallenCityZoneKind.RefugeeStreet:
                Raised(room, left + 760, ground - 68, 240);
                break;
            case FallenCityZoneKind.MarketRuins:
                Raised(room, left + 240, ground - 58, 210);
                Raised(room, left + 505, ground - 112, 230);
                Raised(room, left + 790, ground - 154, 250);
                break;
            case FallenCityZoneKind.WatchDistrict:
                Raised(room, left + 250, ground - 72, 210);
                Raised(room, left + 720, ground - 120, 260);
                break;
            case FallenCityZoneKind.BurnedQuarter:
                Raised(room, left + 500, ground - 66, 260);
                break;
            case FallenCityZoneKind.NobleAvenue:
                Raised(room, left + 190, ground - 60, 220);
                Raised(room, left + 680, ground - 108, 280);
                break;
            case FallenCityZoneKind.ExecutionSquare:
                Raised(room, left + 465, ground - 72, 320);
                break;
            case FallenCityZoneKind.RoyalGuardBarracks:
                Raised(room, left + 240, ground - 66, 230);
                Raised(room, left + 820, ground - 66, 250);
                break;
            case FallenCityZoneKind.PalaceApproach:
                Raised(room, left + 180, ground - 48, 180);
                Raised(room, left + 410, ground - 94, 210);
                Raised(room, left + 680, ground - 138, 220);
                Raised(room, left + 970, ground - 92, 210);
                break;
            case FallenCityZoneKind.RuinedThronePlaza:
                Raised(room, left + 210, ground - 40, 190);
                Raised(room, left + width - 420, ground - 40, 190);
                break;
        }
    }

    private static void AddEncounters(
        IReadOnlyList<FallenCityZoneDefinition> zones,
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets)
    {
        Add(encounters, sockets, zones[0], "outer-gate-patrol", 150, 760,
            EnemyTheme.CityMilitia, EncounterDifficulty.Low, 2,
            (SpawnSocketRole.GroundMelee, 520, -1, EnemyType.FallenMilitiaman),
            (SpawnSocketRole.GroundMelee, 850, -1, EnemyType.FallenMilitiaman));
        Add(encounters, sockets, zones[1], "refugee-plague", 130, 920,
            EnemyTheme.CityPlague, EncounterDifficulty.Standard, 3,
            (SpawnSocketRole.GroundMelee, 470, -1, EnemyType.RabidHound),
            (SpawnSocketRole.Heavy, 740, -1, EnemyType.PlagueCarrier),
            (SpawnSocketRole.GroundMelee, 1030, -1, EnemyType.FallenMilitiaman));
        Add(encounters, sockets, zones[2], "market-ambush", 140, 1000,
            EnemyTheme.CityMilitia, EncounterDifficulty.Hard, 4,
            (SpawnSocketRole.GroundMelee, 420, -1, EnemyType.RabidHound),
            (SpawnSocketRole.Ranged, 620, -112, EnemyType.RuinedCrossbowman),
            (SpawnSocketRole.GroundMelee, 920, -1, EnemyType.FallenMilitiaman));
        Add(encounters, sockets, zones[3], "watch-bell", 120, 900,
            EnemyTheme.CityWatch, EncounterDifficulty.Hard, 4,
            (SpawnSocketRole.GuardPost, 440, -1, EnemyType.BellWatcher),
            (SpawnSocketRole.Ranged, 840, -120, EnemyType.RuinedCrossbowman),
            (SpawnSocketRole.GroundMelee, 1010, -1, EnemyType.FallenMilitiaman));
        Add(encounters, sockets, zones[4], "burned-quarter", 140, 950,
            EnemyTheme.CityAsh, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Heavy, 450, -1, EnemyType.PlagueCarrier),
            (SpawnSocketRole.Elite, 820, -1, EnemyType.AshKnight));
        Add(encounters, sockets, zones[5], "noble-guard", 150, 1000,
            EnemyTheme.CityRoyal, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Elite, 560, -1, EnemyType.RoyalGuard),
            (SpawnSocketRole.Ranged, 820, -108, EnemyType.RuinedCrossbowman),
            (SpawnSocketRole.GuardPost, 1110, -1, EnemyType.BellWatcher));
        Add(encounters, sockets, zones[6], "execution-square", 130, 980,
            EnemyTheme.CityExecution, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Heavy, 580, -1, EnemyType.StreetExecutioner),
            (SpawnSocketRole.Elite, 850, -1, EnemyType.WarBishop));
        Add(encounters, sockets, zones[7], "royal-barracks", 160, 1080,
            EnemyTheme.CityRoyal, EncounterDifficulty.Elite, 4,
            (SpawnSocketRole.Elite, 520, -1, EnemyType.RoyalGuard),
            (SpawnSocketRole.GroundMelee, 800, -1, EnemyType.RabidHound),
            (SpawnSocketRole.Elite, 1130, -1, EnemyType.RoyalGuard));
        Add(encounters, sockets, zones[8], "palace-vanguard", 140, 1050,
            EnemyTheme.CityAsh, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Elite, 560, -1, EnemyType.AshKnight),
            (SpawnSocketRole.Elite, 850, -1, EnemyType.WarBishop),
            (SpawnSocketRole.Elite, 1110, -1, EnemyType.RoyalGuard));
        Add(encounters, sockets, zones[9], "broken-king", 140, 1480,
            EnemyTheme.BrokenKing, EncounterDifficulty.Boss, 1,
            (SpawnSocketRole.Boss, 900, -1, EnemyType.BrokenKing));
    }

    private static void Add(
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets,
        FallenCityZoneDefinition zone,
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
            id,
            zone.ZoneId,
            activation,
            new Rectangle(activation.Left, zone.GroundY - 100, activation.Width, 100),
            theme,
            difficulty,
            Array.ConvertAll(data, item => item.type),
            cap,
            difficulty == EncounterDifficulty.Elite,
            difficulty == EncounterDifficulty.Boss,
            false,
            true));
        int socketIndex = 0;
        foreach (var item in data)
        {
            float y = item.y < 0 ? zone.GroundY + item.y : zone.Bounds.Top + item.y;
            var position = new Vector2(zone.Bounds.Left + item.x, y);
            sockets.Add(new SpawnSocket(
                $"{id}-{++socketIndex:00}",
                id,
                item.role,
                position,
                new Rectangle((int)position.X - 90, (int)position.Y - 120, 180, 150),
                new[] { item.type },
                true));
        }
    }

    private static List<WildForestSpawnProfile> CreateProfiles(
        IReadOnlyList<FallenCityZoneDefinition> zones)
    {
        EnemyType[][] roster =
        {
            new[] { EnemyType.FallenMilitiaman },
            new[] { EnemyType.RabidHound, EnemyType.PlagueCarrier, EnemyType.FallenMilitiaman },
            new[] { EnemyType.RabidHound, EnemyType.RuinedCrossbowman, EnemyType.FallenMilitiaman },
            new[] { EnemyType.BellWatcher, EnemyType.RuinedCrossbowman, EnemyType.FallenMilitiaman },
            new[] { EnemyType.PlagueCarrier, EnemyType.AshKnight },
            new[] { EnemyType.RoyalGuard, EnemyType.RuinedCrossbowman, EnemyType.BellWatcher },
            new[] { EnemyType.StreetExecutioner, EnemyType.WarBishop },
            new[] { EnemyType.RoyalGuard, EnemyType.RabidHound },
            new[] { EnemyType.AshKnight, EnemyType.WarBishop, EnemyType.RoyalGuard },
            new[] { EnemyType.BrokenKing }
        };
        var profiles = new List<WildForestSpawnProfile>(ZoneCount);
        for (int index = 0; index < ZoneCount; index++)
        {
            int[] weights = new int[roster[index].Length];
            for (int item = 0; item < weights.Length; item++)
                weights[item] = 100 / weights.Length;
            weights[0] += 100 - Sum(weights);
            profiles.Add(new WildForestSpawnProfile(
                $"{zones[index].ZoneId}-profile",
                zones[index].ZoneId,
                roster[index],
                weights,
                index == 9 ? 1 : index is 2 or 3 or 7 ? 4 : 3,
                260f,
                true,
                false,
                index >= 4 ? 440 : 260));
        }
        return profiles;
    }

    private static List<WildForestRouteFeature> CreateRouteFeatures(
        IReadOnlyList<FallenCityZoneDefinition> zones) => new()
    {
        Feature("refugee-alley", zones[1], WildForestRouteFeatureKind.SideBranch, 720, -118, 300, 110),
        Feature("market-rooftops", zones[2], WildForestRouteFeatureKind.SideBranch, 220, -190, 850, 180),
        Feature("watchtower-pass", zones[3], WildForestRouteFeatureKind.Shortcut, 690, -165, 300, 150),
        Feature("burned-cellar", zones[4], WildForestRouteFeatureKind.Secret, 510, -120, 260, 110),
        Feature("noble-balcony", zones[5], WildForestRouteFeatureKind.SideBranch, 650, -155, 340, 145),
        Feature("gallows-cache", zones[6], WildForestRouteFeatureKind.Secret, 455, -126, 340, 116),
        Feature("barracks-wall", zones[7], WildForestRouteFeatureKind.Shortcut, 800, -122, 300, 112),
        Feature("palace-rampart", zones[8], WildForestRouteFeatureKind.SideBranch, 400, -190, 800, 180),
        Feature("royal-records", zones[8], WildForestRouteFeatureKind.Secret, 1010, -140, 220, 130)
    };

    private static WildForestRouteFeature Feature(
        string id,
        FallenCityZoneDefinition zone,
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

    private static List<CityAlarmObjectDefinition> CreateAlarmObjects(
        IReadOnlyList<FallenCityZoneDefinition> zones) => new()
    {
        new("watch-bell-post", zones[3].ZoneId,
            new Vector2(zones[3].Bounds.Left + 230, zones[3].GroundY)),
        new("noble-warning-bell", zones[5].ZoneId,
            new Vector2(zones[5].Bounds.Left + 1120, zones[5].GroundY)),
        new("barracks-alarm", zones[7].ZoneId,
            new Vector2(zones[7].Bounds.Left + 1180, zones[7].GroundY))
    };

    private static List<CollapsingStructureDefinition> CreateStructures(
        IReadOnlyList<FallenCityZoneDefinition> zones) => new()
    {
        new("market-weak-balcony", zones[2].ZoneId,
            CollapsingStructureKind.WeakBalcony,
            new Rectangle(zones[2].Bounds.Left + 775, zones[2].GroundY - 178, 285, 24),
            true, hasCollision: true),
        new("watch-broken-stair", zones[3].ZoneId,
            CollapsingStructureKind.BrokenStair,
            new Rectangle(zones[3].Bounds.Left + 690, zones[3].GroundY - 142, 300, 28),
            true, hasCollision: true),
        new("burned-barricade", zones[4].ZoneId,
            CollapsingStructureKind.DestructibleBarricade,
            new Rectangle(zones[4].Bounds.Left + 780, zones[4].GroundY - 70, 34, 70), true),
        new("barracks-collapsing-wall", zones[7].ZoneId,
            CollapsingStructureKind.CollapsingWall,
            new Rectangle(zones[7].Bounds.Left + 750, zones[7].GroundY - 120, 42, 120), true),
        new("palace-cracked-bridge", zones[8].ZoneId,
            CollapsingStructureKind.CrackedBridge,
            new Rectangle(zones[8].Bounds.Left + 675, zones[8].GroundY - 162, 245, 24),
            true, hasCollision: true)
    };

    private static List<CityReinforcementDefinition> CreateReinforcements(
        IReadOnlyList<FallenCityZoneDefinition> zones) => new()
    {
        Reinforcement(2, zones[3], "alarm-patrol-2", 1040, EnemyType.FallenMilitiaman),
        Reinforcement(3, zones[5], "alarm-crossbow-3", 740, EnemyType.RuinedCrossbowman, -108),
        Reinforcement(3, zones[8], "alarm-royal-3", 1180, EnemyType.RoyalGuard)
    };

    private static CityReinforcementDefinition Reinforcement(
        int level,
        FallenCityZoneDefinition zone,
        string id,
        int x,
        EnemyType type,
        int y = -1)
    {
        float top = y < 0 ? zone.GroundY + y : zone.Bounds.Top + y;
        var position = new Vector2(zone.Bounds.Left + x, top);
        return new CityReinforcementDefinition(
            level,
            new SpawnSocket(
                id,
                id,
                type == EnemyType.RuinedCrossbowman
                    ? SpawnSocketRole.Ranged
                    : SpawnSocketRole.GuardPost,
                position,
                new Rectangle((int)position.X - 100, (int)position.Y - 130, 200, 160),
                new[] { type },
                true));
    }

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
