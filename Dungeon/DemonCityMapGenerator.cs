using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

/// <summary>
/// Map 6 city structure and its incrementally assigned enemy-number sockets.
/// Later sockets remain neutral until their dedicated design passes land.
/// </summary>
public sealed class DemonCityMapGenerator
{
    public const int ZoneCount = 10;
    public const int WorldWidth = 18308;
    private const int Margin = 80;
    private const int TransitionWidth = 112;

    public DungeonMap Generate()
    {
        int[] widths =
            { 1500, 1500, 2100, 1900, 1900, 1900, 1800, 1600, 2200, 900 };
        int[] grounds =
            { 676, 664, 684, 670, 646, 590, 612, 596, 616, 608 };
        string[] keys =
        {
            "demon_city.zone.outer_approach",
            "demon_city.zone.city_entrance",
            "demon_city.zone.lower_district",
            "demon_city.zone.market_depths",
            "demon_city.zone.industrial_ritual_district",
            "demon_city.zone.high_city",
            "demon_city.zone.elite_district",
            "demon_city.zone.palace_approach",
            "demon_city.zone.main_arena",
            "demon_city.zone.map_seven_exit"
        };
        DemonCityZoneKind[] kinds =
            (DemonCityZoneKind[])Enum.GetValues(typeof(DemonCityZoneKind));
        var rooms = new List<DungeonRoom>(ZoneCount);
        var corridors = new List<Rectangle>(ZoneCount - 1);
        var zones = new List<DemonCityZoneDefinition>(ZoneCount);
        int x = Margin;

        for (int index = 0; index < ZoneCount; index++)
        {
            RoomType type = index == 0 ? RoomType.Start :
                index == 3 ? RoomType.Treasure :
                index == 8 ? RoomType.Boss :
                index == 9 ? RoomType.Exit : RoomType.Enemy;
            var room = new DungeonRoom(
                index,
                new Rectangle(x, Margin, widths[index], DungeonGenerator.RoomHeight),
                type)
            {
                GroundY = grounds[index]
            };
            AddGeometry(room, kinds[index]);
            rooms.Add(room);
            zones.Add(new DemonCityZoneDefinition(
                $"demon-city-{index + 1:00}", keys[index], kinds[index],
                room.Bounds, room.GroundY));

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(
                    previous.Bounds.Right, Margin, TransitionWidth,
                    DungeonGenerator.RoomHeight);
                corridors.Add(corridor);
                AddTransition(previous, corridor, previous.GroundY, room.GroundY);
            }
            x += widths[index] + (index < ZoneCount - 1 ? TransitionWidth : 0);
        }

        List<SpawnSocket> sockets = CreatePlannedSockets(zones);
        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(Margin, Margin, WorldWidth, DungeonGenerator.RoomHeight),
            isDemonCity: true,
            encounterZones: CreateEnemyEncounterZones(zones),
            spawnSockets: sockets,
            demonCityZones: zones,
            demonCityProps: CreateProps(zones),
            demonCityCheckpoints: CreateCheckpoints(zones),
            demonCityEventSpaces: CreateEventSpaces(zones),
            authoredExitPosition: new Vector2(
                rooms[^1].Bounds.Right - 145f, rooms[^1].GroundY - 28f));
    }

    private static void AddGeometry(DungeonRoom room, DemonCityZoneKind kind)
    {
        int left = room.Bounds.Left;
        int ground = room.GroundY;
        Ground(room, left, room.Bounds.Width, ground);

        switch (kind)
        {
            case DemonCityZoneKind.OuterApproach:
                Raised(room, left + 430, ground - 32, 220);
                Raised(room, left + 1120, ground - 58, 250);
                break;
            case DemonCityZoneKind.CityEntrance:
                Raised(room, left + 290, ground - 72, 300);
                Raised(room, left + 1110, ground - 46, 260);
                break;
            case DemonCityZoneKind.LowerDistrict:
                Raised(room, left + 360, ground - 48, 300);
                Raised(room, left + 910, ground - 104, 370);
                Raised(room, left + 1640, ground - 62, 340);
                break;
            case DemonCityZoneKind.MarketDepths:
                Raised(room, left + 310, ground - 62, 260);
                Raised(room, left + 800, ground - 36, 300);
                Raised(room, left + 1410, ground - 92, 320);
                break;
            case DemonCityZoneKind.IndustrialRitualDistrict:
                Raised(room, left + 270, ground - 70, 300);
                Raised(room, left + 790, ground - 122, 390);
                Raised(room, left + 1390, ground - 78, 280);
                break;
            case DemonCityZoneKind.HighCity:
                Raised(room, left + 280, ground - 82, 340);
                Raised(room, left + 760, ground - 150, 330);
                Raised(room, left + 1280, ground - 90, 360);
                break;
            case DemonCityZoneKind.EliteDistrict:
                Raised(room, left + 330, ground - 54, 300);
                Raised(room, left + 1170, ground - 54, 300);
                break;
            case DemonCityZoneKind.PalaceApproach:
                Raised(room, left + 250, ground - 38, 220);
                Raised(room, left + 1130, ground - 38, 220);
                break;
            // The arena and sealed exit remain collision-clean by design.
            case DemonCityZoneKind.MainBossArena:
            case DemonCityZoneKind.MapSevenExit:
                break;
        }
    }

    private static List<SpawnSocket> CreatePlannedSockets(
        IReadOnlyList<DemonCityZoneDefinition> zones)
    {
        var sockets = new List<SpawnSocket>(11);
        AddPlannedSocket(sockets, zones[1], 51, 620,
            SpawnSocketRole.GroundMelee, EnemyType.DemonCityEnforcer,
            introEligible: true);
        AddPlannedSocket(sockets, zones[2], 52, 760,
            SpawnSocketRole.Ranged, EnemyType.SoulLanternKeeper,
            introEligible: true);
        AddPlannedSocket(sockets, zones[2], 53, 1450,
            SpawnSocketRole.GroundMelee, EnemyType.AshbladeStalker,
            introEligible: true);
        AddPlannedSocket(sockets, zones[3], 54, 690,
            SpawnSocketRole.Ranged, EnemyType.ChainCollector,
            introEligible: true);
        AddPlannedSocket(sockets, zones[4], 55, 680,
            SpawnSocketRole.Heavy, EnemyType.BloodworksServitor,
            introEligible: true);
        AddPlannedSocket(sockets, zones[4], 56, 1260,
            SpawnSocketRole.Ranged, EnemyType.SigilCantor,
            introEligible: true);
        AddPlannedSocket(sockets, zones[5], 57, 690,
            SpawnSocketRole.Elite, EnemyType.VeilWarden,
            introEligible: true);
        AddPlannedSocket(sockets, zones[6], 58, 790,
            SpawnSocketRole.Elite, EnemyType.GloomwingHarrier,
            introEligible: true);
        AddPlannedSocket(sockets, zones[6], 59, 1570,
            SpawnSocketRole.Elite, EnemyType.CathedralButcher,
            introEligible: true);
        AddPlannedSocket(sockets, zones[7], 60, 800,
            SpawnSocketRole.Boss, EnemyType.AbyssalMagistrate,
            introEligible: true);

        DemonCityZoneDefinition arena = zones[8];
        var bossPosition = new Vector2(arena.Bounds.Center.X, arena.GroundY);
        sockets.Add(new SpawnSocket(
            "map6-enemy-61-malachar", arena.ZoneId, SpawnSocketRole.Boss,
            bossPosition,
            new Rectangle((int)bossPosition.X - 180, (int)bossPosition.Y - 190,
                360, 220),
            new[] { EnemyType.Malachar }, introEligible: true));
        return sockets;
    }

    private static void AddPlannedSocket(
        List<SpawnSocket> sockets,
        DemonCityZoneDefinition zone,
        int enemyNumber,
        int offsetX,
        SpawnSocketRole role = SpawnSocketRole.Unassigned,
        EnemyType? enemyType = null,
        bool introEligible = false)
    {
        var position = new Vector2(zone.Bounds.Left + offsetX, zone.GroundY);
        sockets.Add(new SpawnSocket(
            $"map6-enemy-{enemyNumber}-placeholder",
            zone.ZoneId,
            role,
            position,
            new Rectangle((int)position.X - 110, (int)position.Y - 140, 220, 170),
            enemyType.HasValue
                ? new[] { enemyType.Value }
                : Array.Empty<EnemyType>(),
            introEligible));
    }

    private static List<EncounterZone> CreateEnemyEncounterZones(
        IReadOnlyList<DemonCityZoneDefinition> zones)
    {
        DemonCityZoneDefinition entrance = zones[1];
        DemonCityZoneDefinition lower = zones[2];
        DemonCityZoneDefinition market = zones[3];
        DemonCityZoneDefinition industrial = zones[4];
        DemonCityZoneDefinition highCity = zones[5];
        DemonCityZoneDefinition eliteDistrict = zones[6];
        DemonCityZoneDefinition palaceApproach = zones[7];
        DemonCityZoneDefinition arena = zones[8];
        return new List<EncounterZone>
        {
            new(
                entrance.ZoneId,
                entrance.ZoneId,
                new Rectangle(
                    entrance.Bounds.Left + 120,
                    entrance.Bounds.Top + 60,
                    entrance.Bounds.Width - 240,
                    entrance.Bounds.Height - 95),
                new Rectangle(
                    entrance.Bounds.Left + 120,
                    entrance.GroundY - 110,
                    entrance.Bounds.Width - 240,
                    110),
                EnemyTheme.DemonCityEnforcement,
                EncounterDifficulty.Standard,
                new[] { EnemyType.DemonCityEnforcer },
                maxConcurrentEnemies: 1,
                introEligible: true),
            new(
                lower.ZoneId,
                lower.ZoneId,
                new Rectangle(
                    lower.Bounds.Left + 120,
                    lower.Bounds.Top + 60,
                    lower.Bounds.Width - 240,
                    lower.Bounds.Height - 95),
                new Rectangle(
                    lower.Bounds.Left + 120,
                    lower.GroundY - 110,
                    lower.Bounds.Width - 240,
                    110),
                EnemyTheme.DemonCityStreet,
                EncounterDifficulty.Standard,
                new[] { EnemyType.SoulLanternKeeper, EnemyType.AshbladeStalker },
                maxConcurrentEnemies: 2,
                introEligible: true),
            new(
                market.ZoneId,
                market.ZoneId,
                new Rectangle(
                    market.Bounds.Left + 120,
                    market.Bounds.Top + 60,
                    market.Bounds.Width - 240,
                    market.Bounds.Height - 95),
                new Rectangle(
                    market.Bounds.Left + 120,
                    market.GroundY - 110,
                    market.Bounds.Width - 240,
                    110),
                EnemyTheme.DemonCityMarket,
                EncounterDifficulty.Standard,
                new[] { EnemyType.ChainCollector },
                maxConcurrentEnemies: 1,
                introEligible: true),
            new(
                industrial.ZoneId,
                industrial.ZoneId,
                new Rectangle(
                    industrial.Bounds.Left + 120,
                    industrial.Bounds.Top + 60,
                    industrial.Bounds.Width - 240,
                    industrial.Bounds.Height - 95),
                new Rectangle(
                    industrial.Bounds.Left + 120,
                    industrial.GroundY - 130,
                    industrial.Bounds.Width - 240,
                    130),
                EnemyTheme.DemonCityRitual,
                EncounterDifficulty.Hard,
                new[]
                {
                    EnemyType.BloodworksServitor,
                    EnemyType.SigilCantor
                },
                maxConcurrentEnemies: 2,
                introEligible: true),
            new(
                highCity.ZoneId,
                highCity.ZoneId,
                new Rectangle(
                    highCity.Bounds.Left + 120,
                    highCity.Bounds.Top + 60,
                    highCity.Bounds.Width - 240,
                    highCity.Bounds.Height - 95),
                new Rectangle(
                    highCity.Bounds.Left + 120,
                    highCity.GroundY - 150,
                    highCity.Bounds.Width - 240,
                    150),
                EnemyTheme.DemonCityElite,
                EncounterDifficulty.Elite,
                new[] { EnemyType.VeilWarden },
                maxConcurrentEnemies: 1,
                isEliteZone: true,
                introEligible: true),
            new(
                "demon-city-gloomwing-harrier",
                eliteDistrict.ZoneId,
                new Rectangle(
                    eliteDistrict.Bounds.Left + 120,
                    eliteDistrict.Bounds.Top + 60,
                    900,
                    eliteDistrict.Bounds.Height - 95),
                new Rectangle(
                    eliteDistrict.Bounds.Left + 120,
                    eliteDistrict.GroundY - 190,
                    900,
                    190),
                EnemyTheme.DemonCityElite,
                EncounterDifficulty.Elite,
                new[] { EnemyType.GloomwingHarrier },
                maxConcurrentEnemies: 1,
                isEliteZone: true,
                introEligible: true),
            new(
                "demon-city-cathedral-butcher",
                eliteDistrict.ZoneId,
                new Rectangle(
                    eliteDistrict.Bounds.Left + 1030,
                    eliteDistrict.Bounds.Top + 60,
                    eliteDistrict.Bounds.Width - 1150,
                    eliteDistrict.Bounds.Height - 95),
                new Rectangle(
                    eliteDistrict.Bounds.Left + 1030,
                    eliteDistrict.GroundY - 190,
                    eliteDistrict.Bounds.Width - 1150,
                    190),
                EnemyTheme.DemonCityElite,
                EncounterDifficulty.Elite,
                new[] { EnemyType.CathedralButcher },
                maxConcurrentEnemies: 1,
                isEliteZone: true,
                introEligible: true),
            new(
                "demon-city-abyssal-magistrate",
                palaceApproach.ZoneId,
                new Rectangle(
                    palaceApproach.Bounds.Left + 120,
                    palaceApproach.Bounds.Top + 60,
                    palaceApproach.Bounds.Width - 240,
                    palaceApproach.Bounds.Height - 95),
                new Rectangle(
                    palaceApproach.Bounds.Left + 120,
                    palaceApproach.GroundY - 190,
                    palaceApproach.Bounds.Width - 240,
                    190),
                EnemyTheme.DemonCityElite,
                EncounterDifficulty.Boss,
                new[] { EnemyType.AbyssalMagistrate },
                maxConcurrentEnemies: 1,
                isBossZone: true,
                introEligible: true),
            new(
                "demon-city-malachar",
                arena.ZoneId,
                new Rectangle(
                    arena.Bounds.Left + 120,
                    arena.Bounds.Top + 60,
                    arena.Bounds.Width - 240,
                    arena.Bounds.Height - 95),
                new Rectangle(
                    arena.Bounds.Left + 120,
                    arena.GroundY - 210,
                    arena.Bounds.Width - 240,
                    210),
                EnemyTheme.DemonCityElite,
                EncounterDifficulty.Boss,
                new[] { EnemyType.Malachar },
                maxConcurrentEnemies: 1,
                isBossZone: true,
                introEligible: true)
        };
    }

    private static List<DemonCityCheckpointDefinition> CreateCheckpoints(
        IReadOnlyList<DemonCityZoneDefinition> zones) => new()
    {
        Checkpoint("city-entry-secured", zones[1], 1400),
        Checkpoint("lower-city-crossed", zones[3], 1780),
        Checkpoint("industrial-district-crossed", zones[4], 1760),
        Checkpoint("high-city-reached", zones[5], 180),
        Checkpoint("palace-threshold", zones[7], 1480)
    };

    private static DemonCityCheckpointDefinition Checkpoint(
        string id,
        DemonCityZoneDefinition zone,
        int offsetX) => new(
            id, zone.ZoneId,
            new Vector2(zone.Bounds.Left + offsetX, zone.GroundY - 28f));

    private static List<DemonCityEventSpaceDefinition> CreateEventSpaces(
        IReadOnlyList<DemonCityZoneDefinition> zones) => new()
    {
        Event("city-gate-event-space", zones[1], DemonCityEventSpaceKind.PlazaEncounter,
            180, 1200),
        Event("lower-street-event-space", zones[2], DemonCityEventSpaceKind.StreetEncounter,
            240, 1640),
        Event("market-event-space", zones[3], DemonCityEventSpaceKind.PlazaEncounter,
            220, 1460),
        Event("industrial-ritual-event-space", zones[4],
            DemonCityEventSpaceKind.RitualChamber, 180, 1540),
        Event("high-city-bridge-event-space", zones[5],
            DemonCityEventSpaceKind.ElevatedBridge, 170, 1560),
        Event("elite-courtyard-event-space", zones[6],
            DemonCityEventSpaceKind.EliteCourtyard, 190, 1420),
        Event("palace-approach-event-space", zones[7],
            DemonCityEventSpaceKind.PalaceHall, 150, 1300),
        Event("map6-main-boss-arena", zones[8], DemonCityEventSpaceKind.BossArena,
            120, 1960)
    };

    private static DemonCityEventSpaceDefinition Event(
        string id,
        DemonCityZoneDefinition zone,
        DemonCityEventSpaceKind kind,
        int offsetX,
        int width) => new(
            id, zone.ZoneId, kind,
            new Rectangle(zone.Bounds.Left + offsetX, zone.Bounds.Top + 70,
                width, zone.Bounds.Height - 120));

    private static List<DemonCityPropDefinition> CreateProps(
        IReadOnlyList<DemonCityZoneDefinition> z)
    {
        var props = new List<DemonCityPropDefinition>();
        void Add(int zone, string id, DemonCityPropKind kind, int x, int y,
            int width, int height,
            DemonCityLayer layer = DemonCityLayer.Midground) =>
            props.Add(new DemonCityPropDefinition(
                id, z[zone].ZoneId, kind,
                new Rectangle(z[zone].Bounds.Left + x, z[zone].GroundY + y,
                    width, height), layer));

        Add(0, "approach-towers", DemonCityPropKind.Tower, 120, -390, 260, 390,
            DemonCityLayer.Background);
        Add(0, "approach-bridge", DemonCityPropKind.Bridge, 560, -170, 650, 95);
        Add(0, "approach-debris", DemonCityPropKind.Debris, 1220, -55, 210, 55,
            DemonCityLayer.Foreground);
        Add(1, "entrance-wall", DemonCityPropKind.BuildingFacade,
            80, -480, 1340, 480, DemonCityLayer.Background);
        Add(1, "entrance-arch", DemonCityPropKind.Arch, 350, -470, 800, 470);
        Add(1, "entrance-statue-left", DemonCityPropKind.Statue,
            140, -250, 130, 250);
        Add(1, "entrance-statue-right", DemonCityPropKind.Statue,
            1230, -250, 130, 250);
        Add(2, "lower-facades", DemonCityPropKind.BuildingFacade,
            100, -390, 820, 390, DemonCityLayer.Background);
        Add(2, "lower-windows", DemonCityPropKind.WindowStack,
            1030, -350, 360, 310);
        Add(2, "lower-balcony", DemonCityPropKind.Balcony, 1480, -240, 460, 95);
        Add(2, "lower-rails", DemonCityPropKind.Rail, 660, -76, 430, 76,
            DemonCityLayer.Foreground);
        Add(3, "market-facade", DemonCityPropKind.BuildingFacade,
            90, -420, 610, 420, DemonCityLayer.Background);
        Add(3, "market-stalls-a", DemonCityPropKind.MarketStall,
            300, -105, 250, 105);
        Add(3, "market-stalls-b", DemonCityPropKind.MarketStall,
            870, -120, 290, 120);
        Add(3, "market-roofs", DemonCityPropKind.Roofline,
            1220, -360, 570, 140, DemonCityLayer.Background);
        Add(3, "market-debris", DemonCityPropKind.Debris,
            1530, -45, 190, 45, DemonCityLayer.Foreground);
        Add(4, "industrial-works", DemonCityPropKind.BuildingFacade,
            80, -470, 1740, 470, DemonCityLayer.Background);
        Add(4, "industrial-furnace-left", DemonCityPropKind.Furnace,
            170, -220, 230, 220);
        Add(4, "industrial-furnace-right", DemonCityPropKind.Furnace,
            1480, -250, 240, 250);
        Add(4, "industrial-pipe-bank", DemonCityPropKind.Pipe,
            480, -360, 500, 320);
        Add(4, "industrial-chain", DemonCityPropKind.Chain,
            1130, -500, 50, 390, DemonCityLayer.Foreground);
        Add(4, "industrial-ritual-circle", DemonCityPropKind.RitualCircle,
            820, -24, 380, 24);
        Add(4, "industrial-rail", DemonCityPropKind.Rail,
            1250, -80, 210, 80, DemonCityLayer.Foreground);
        Add(5, "high-city-bridge", DemonCityPropKind.Bridge, 120, -260, 700, 95);
        Add(5, "high-city-tower", DemonCityPropKind.Tower, 880, -540, 340, 540,
            DemonCityLayer.Background);
        Add(5, "high-city-balcony", DemonCityPropKind.Balcony,
            1300, -300, 470, 100);
        Add(5, "high-city-roofs", DemonCityPropKind.Roofline,
            300, -450, 560, 130, DemonCityLayer.Background);
        Add(5, "high-city-rail", DemonCityPropKind.Rail, 620, -82, 380, 82,
            DemonCityLayer.Foreground);
        Add(6, "elite-pillars-left", DemonCityPropKind.Pillar,
            120, -450, 120, 450, DemonCityLayer.Background);
        Add(6, "elite-facade", DemonCityPropKind.BuildingFacade,
            330, -490, 1140, 490, DemonCityLayer.Background);
        Add(6, "elite-pillars-right", DemonCityPropKind.Pillar,
            1560, -450, 120, 450, DemonCityLayer.Background);
        Add(6, "elite-banners-left", DemonCityPropKind.Banner, 500, -400, 110, 250);
        Add(6, "elite-banners-right", DemonCityPropKind.Banner, 1190, -400, 110, 250);
        Add(6, "elite-statue", DemonCityPropKind.Statue, 820, -320, 160, 320);
        Add(7, "palace-pillars-left", DemonCityPropKind.Pillar,
            100, -520, 130, 520, DemonCityLayer.Background);
        Add(7, "palace-arch", DemonCityPropKind.Arch, 330, -540, 940, 540);
        Add(7, "palace-pillars-right", DemonCityPropKind.Pillar,
            1370, -520, 130, 520, DemonCityLayer.Background);
        Add(7, "palace-statue-left", DemonCityPropKind.Statue, 300, -320, 150, 320);
        Add(7, "palace-statue-right", DemonCityPropKind.Statue, 1150, -320, 150, 320);
        Add(8, "arena-pillars-left", DemonCityPropKind.Pillar, 120, -540, 150, 540,
            DemonCityLayer.Background);
        Add(8, "arena-pillars-right", DemonCityPropKind.Pillar, 1930, -540, 150, 540,
            DemonCityLayer.Background);
        Add(8, "arena-banners-left", DemonCityPropKind.Banner, 390, -450, 110, 270);
        Add(8, "arena-banners-right", DemonCityPropKind.Banner, 1700, -450, 110, 270);
        Add(9, "map-seven-sealed-gate", DemonCityPropKind.SealedGate,
            290, -510, 320, 510);
        return props;
    }

    private static void Ground(DungeonRoom room, int x, int width, int y) =>
        room.AddPlatform(new Platform(
            new Rectangle(x, y, width, room.Bounds.Bottom - y),
            PlatformKind.Ground, room.Id));

    private static void Raised(DungeonRoom room, int x, int y, int width) =>
        room.AddPlatform(new Platform(
            new Rectangle(x, y, width, 16), PlatformKind.Raised, room.Id));

    private static void AddTransition(
        DungeonRoom room,
        Rectangle corridor,
        int fromGround,
        int toGround)
    {
        int quarter = corridor.Width / 4;
        for (int step = 0; step < 4; step++)
        {
            int y = (int)MathF.Round(MathHelper.Lerp(
                fromGround, toGround, (step + 1f) / 5f));
            int width = step == 3 ? corridor.Width - quarter * 3 : quarter;
            Ground(room, corridor.Left + quarter * step, width, y);
        }
    }
}
