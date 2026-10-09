using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
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
    public const int AuthoredWildForestSectionCount = 8;
    public const int AuthoredWildForestWidth = 10148;
    public const int AuthoredTransitionWidth = 64;

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
    /// Builds the deliberate eight-section Map 1 route.  Geometry, pacing,
    /// landmarks and future encounter origins are authored; no enemy is
    /// instantiated by this method.
    /// </summary>
    public DungeonMap GenerateWildForest()
    {
        int[] widths = { 1000, 1200, 1150, 1050, 1250, 1300, 1200, 1550 };
        int[] groundY = { 680, 672, 680, 672, 684, 676, 680, 680 };
        RoomType[] roomTypes =
        {
            RoomType.Start,
            RoomType.Enemy,
            RoomType.Enemy,
            RoomType.Enemy,
            RoomType.Enemy,
            RoomType.Treasure,
            RoomType.Enemy,
            RoomType.Boss
        };
        WildForestSectionKind[] kinds =
        {
            WildForestSectionKind.ForestOutskirts,
            WildForestSectionKind.GoblinEncampment,
            WildForestSectionKind.Webwood,
            WildForestSectionKind.Thornlands,
            WildForestSectionKind.CorruptedGrove,
            WildForestSectionKind.WarCamp,
            WildForestSectionKind.MothersNest,
            WildForestSectionKind.AncientSanctuary
        };
        string[] names =
        {
            "FOREST OUTSKIRTS",
            "GOBLIN ENCAMPMENT",
            "WEBWOOD",
            "THORNLANDS",
            "CORRUPTED GROVE",
            "WAR CAMP",
            "MOTHER'S NEST",
            "ANCIENT SANCTUARY"
        };
        float[] fog = { .12f, .20f, .36f, .31f, .49f, .30f, .58f, .43f };
        float[] corruption = { .05f, .15f, .23f, .42f, .58f, .34f, .69f, .88f };

        var rooms = new List<DungeonRoom>(AuthoredWildForestSectionCount);
        var corridors = new List<Rectangle>(AuthoredWildForestSectionCount - 1);
        var sections = new List<WildForestSection>(AuthoredWildForestSectionCount);
        int x = WorldMargin;

        for (int index = 0; index < AuthoredWildForestSectionCount; index++)
        {
            var room = new DungeonRoom(
                index,
                new Rectangle(x, WorldMargin, widths[index], RoomHeight),
                roomTypes[index]);
            room.GroundY = groundY[index];
            AddWildForestSectionGeometry(room, kinds[index]);
            rooms.Add(room);
            sections.Add(new WildForestSection(
                $"wild-forest-{index + 1:00}",
                names[index],
                kinds[index],
                room.Bounds,
                groundY[index],
                fog[index],
                corruption[index]));

            if (index > 0)
            {
                DungeonRoom previous = rooms[index - 1];
                previous.ConnectTo(room.Id);
                room.ConnectTo(previous.Id);
                var corridor = new Rectangle(
                    previous.Bounds.Right,
                    WorldMargin,
                    AuthoredTransitionWidth,
                    RoomHeight);
                corridors.Add(corridor);
                AddAuthoredTransition(
                    previous,
                    corridor,
                    previous.GroundY,
                    room.GroundY);
            }

            x += widths[index] +
                (index < AuthoredWildForestSectionCount - 1
                    ? AuthoredTransitionWidth
                    : 0);
        }

        var landmarks = CreateWildForestLandmarks(sections);
        var zones = new List<EncounterZone>();
        var sockets = new List<SpawnSocket>();
        CreateWildForestEncounterHooks(sections, zones, sockets);
        var routeFeatures = CreateWildForestRouteFeatures(sections);
        var spawnProfiles = CreateWildForestSpawnProfiles(sections);
        DungeonRoom arena = rooms[^1];
        Vector2 exitPosition = new(
            arena.Bounds.Right - 120f,
            arena.GroundY - 28f);

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(WorldMargin, WorldMargin, AuthoredWildForestWidth, RoomHeight),
            isAuthoredWildForest: true,
            wildForestSections: sections,
            encounterZones: zones,
            spawnSockets: sockets,
            landmarks: landmarks,
            routeFeatures: routeFeatures,
            spawnProfiles: spawnProfiles,
            exitRoomOverride: arena,
            authoredExitPosition: exitPosition);
    }

    private static void AddWildForestSectionGeometry(
        DungeonRoom room,
        WildForestSectionKind kind)
    {
        int left = room.Bounds.Left;
        int width = room.Bounds.Width;
        switch (kind)
        {
            case WildForestSectionKind.ForestOutskirts:
                AddGround(room, left, 330, 680);
                AddGround(room, left + 330, 270, 664);
                AddGround(room, left + 600, width - 600, 680);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 690, 642, 82, 38),
                    PlatformKind.Obstacle,
                    room.Id));
                break;

            case WildForestSectionKind.GoblinEncampment:
                AddGround(room, left, width, 672);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 760, 574, 230, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 320, 630, 52, 42),
                    PlatformKind.Obstacle,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 1030, 624, 56, 48),
                    PlatformKind.Obstacle,
                    room.Id));
                break;

            case WildForestSectionKind.Webwood:
                AddGround(room, left, 250, 680);
                AddGround(room, left + 250, 650, 672);
                AddGround(room, left + 900, width - 900, 680);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 170, 604, 180, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 355, 526, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 610, 552, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 845, 612, 185, 16),
                    PlatformKind.Raised,
                    room.Id));
                break;

            case WildForestSectionKind.Thornlands:
                AddGround(room, left, 280, 672);
                AddGround(room, left + 280, 240, 688);
                AddGround(room, left + 520, 300, 680);
                AddGround(room, left + 820, width - 820, 672);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 360, 602, 190, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 630, 548, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                break;

            case WildForestSectionKind.CorruptedGrove:
                AddGround(room, left, 330, 684);
                AddGround(room, left + 330, 610, 676);
                AddGround(room, left + 940, width - 940, 684);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 170, 612, 190, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 930, 594, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 480, 628, 48, 48),
                    PlatformKind.Obstacle,
                    room.Id));
                break;

            case WildForestSectionKind.WarCamp:
                AddGround(room, left, width, 676);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 170, 612, 190, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 940, 604, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 330, 624, 48, 52),
                    PlatformKind.Obstacle,
                    room.Id));
                break;

            case WildForestSectionKind.MothersNest:
                AddGround(room, left, width, 680);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 160, 610, 190, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 370, 538, 210, 16),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 610, 486, 280, 18),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(left + 910, 556, 180, 16),
                    PlatformKind.Raised,
                    room.Id));
                break;

            case WildForestSectionKind.AncientSanctuary:
                AddGround(room, left, width, 680);
                room.AddPlatform(new Platform(
                    new Rectangle(left + 190, 636, 170, 14),
                    PlatformKind.Raised,
                    room.Id));
                room.AddPlatform(new Platform(
                    new Rectangle(room.Bounds.Right - 360, 636, 170, 14),
                    PlatformKind.Raised,
                    room.Id));
                break;

            default:
                AddGround(room, left, width, room.GroundY);
                break;
        }
    }

    private static void AddGround(
        DungeonRoom room,
        int left,
        int width,
        int top)
    {
        if (width <= 0)
            return;
        room.AddPlatform(new Platform(
            new Rectangle(left, top, width, room.Bounds.Bottom - top),
            PlatformKind.Ground,
            room.Id));
    }

    private static void AddAuthoredTransition(
        DungeonRoom owner,
        Rectangle corridor,
        int fromY,
        int toY)
    {
        int midpoint = corridor.Width / 2;
        int firstY = (int)MathF.Round(MathHelper.Lerp(fromY, toY, .33f));
        int secondY = (int)MathF.Round(MathHelper.Lerp(fromY, toY, .67f));
        owner.AddPlatform(new Platform(
            new Rectangle(
                corridor.Left,
                firstY,
                midpoint,
                corridor.Bottom - firstY),
            PlatformKind.Transition,
            owner.Id));
        owner.AddPlatform(new Platform(
            new Rectangle(
                corridor.Left + midpoint,
                secondY,
                corridor.Width - midpoint,
                corridor.Bottom - secondY),
            PlatformKind.Transition,
            owner.Id));
    }

    private static List<WildForestLandmark> CreateWildForestLandmarks(
        IReadOnlyList<WildForestSection> sections)
    {
        WildForestSection outskirts = sections[0];
        WildForestSection webwood = sections[2];
        WildForestSection grove = sections[4];
        WildForestSection warCamp = sections[5];
        WildForestSection nest = sections[6];
        WildForestSection arena = sections[7];
        return new List<WildForestLandmark>
        {
            new("fallen-watch-tree", WildForestLandmarkKind.FallenWatchTree,
                outskirts.SectionId,
                new Vector2(outskirts.Bounds.Left + 700f, outskirts.GroundY)),
            new("spider-grove-heart", WildForestLandmarkKind.SpiderGroveDeadTree,
                webwood.SectionId,
                new Vector2(webwood.Bounds.Center.X, webwood.GroundY)),
            new("war-camp-gate", WildForestLandmarkKind.WarCampGate,
                warCamp.SectionId,
                new Vector2(warCamp.Bounds.Left + 190f, warCamp.GroundY)),
            new("gigantic-root-gate", WildForestLandmarkKind.GiganticRootGate,
                grove.SectionId,
                new Vector2(grove.Bounds.Left + 1050f, grove.GroundY)),
            new("mothers-nest-maw", WildForestLandmarkKind.MothersNestMaw,
                nest.SectionId,
                new Vector2(nest.Bounds.Left + 735f, nest.GroundY - 194f)),
            new("treant-arena-heart", WildForestLandmarkKind.TreantArenaHeart,
                arena.SectionId,
                new Vector2(arena.Bounds.Center.X, arena.GroundY))
        };
    }

    private static void CreateWildForestEncounterHooks(
        IReadOnlyList<WildForestSection> sections,
        List<EncounterZone> zones,
        List<SpawnSocket> sockets)
    {
        WildForestSection outskirts = sections[0];
        AddEncounterZone(
            zones, sockets,
            "outskirts-first-wolf", outskirts, 380, 330,
            EnemyTheme.WildPredators, EncounterDifficulty.Low,
            new[] { EnemyType.DireWolf }, 1,
            (SpawnSocketRole.GroundMelee, 650, -1, new[] { EnemyType.DireWolf }));
        AddEncounterZone(
            zones, sockets,
            "outskirts-predators", outskirts, 620, 330,
            EnemyTheme.WildPredators, EncounterDifficulty.Low,
            new[] { EnemyType.DireWolf, EnemyType.BloodBat }, 2,
            (SpawnSocketRole.GroundMelee, 880, -1, new[] { EnemyType.DireWolf }),
            (SpawnSocketRole.Flying, 940, -175, new[] { EnemyType.BloodBat }));

        WildForestSection goblins = sections[1];
        AddEncounterZone(
            zones, sockets,
            "goblin-encampment", goblins, 130, 1010,
            EnemyTheme.GoblinCamp, EncounterDifficulty.Standard,
            new[] { EnemyType.Goblin, EnemyType.GoblinHunter }, 4,
            (SpawnSocketRole.GroundMelee, 290, -1, new[] { EnemyType.Goblin }),
            (SpawnSocketRole.GroundMelee, 610, -1, new[] { EnemyType.Goblin }),
            (SpawnSocketRole.Ranged, 845, -98, new[] { EnemyType.GoblinHunter }),
            (SpawnSocketRole.Ranged, 1080, -1, new[] { EnemyType.GoblinHunter }));

        WildForestSection spiders = sections[2];
        AddEncounterZone(
            zones, sockets,
            "webwood-brood", spiders, 110, 950,
            EnemyTheme.SpiderBrood, EncounterDifficulty.Standard,
            new[] { EnemyType.GiantSpider, EnemyType.Spiderling }, 4,
            (SpawnSocketRole.GroundMelee, 250, -1, new[] { EnemyType.GiantSpider }),
            (SpawnSocketRole.Ambush, 525, -146, new[] { EnemyType.GiantSpider }),
            (SpawnSocketRole.GroundMelee, 800, -1, new[] { EnemyType.GiantSpider }),
            (SpawnSocketRole.Minion, 970, -1, new[] { EnemyType.Spiderling }));

        WildForestSection thorns = sections[3];
        AddEncounterZone(
            zones, sockets,
            "thorn-bed-west", thorns, 130, 390,
            EnemyTheme.ThornCorruption, EncounterDifficulty.Standard,
            new[] { EnemyType.ThornCrawler }, 2,
            (SpawnSocketRole.Ambush, 220, -1, new[] { EnemyType.ThornCrawler }),
            (SpawnSocketRole.Ambush, 430, -1, new[] { EnemyType.ThornCrawler }));
        AddEncounterZone(
            zones, sockets,
            "thorn-bed-east", thorns, 650, 380,
            EnemyTheme.ThornCorruption, EncounterDifficulty.Hard,
            new[] { EnemyType.ThornCrawler }, 2,
            (SpawnSocketRole.Ambush, 720, -1, new[] { EnemyType.ThornCrawler }),
            (SpawnSocketRole.Ambush, 940, -1, new[] { EnemyType.ThornCrawler }));

        WildForestSection grove = sections[4];
        AddEncounterZone(
            zones, sockets,
            "corrupted-grove", grove, 150, 1000,
            EnemyTheme.TreantTerritory, EncounterDifficulty.Hard,
            new[] { EnemyType.CorruptedTreant, EnemyType.BloodBat, EnemyType.ThornCrawler }, 4,
            (SpawnSocketRole.Heavy, 610, -1, new[] { EnemyType.CorruptedTreant }),
            (SpawnSocketRole.Ambush, 350, -1, new[] { EnemyType.ThornCrawler }),
            (SpawnSocketRole.Flying, 870, -185, new[] { EnemyType.BloodBat }),
            (SpawnSocketRole.Ambush, 1050, -1, new[] { EnemyType.ThornCrawler }));

        WildForestSection warCamp = sections[5];
        AddEncounterZone(
            zones, sockets,
            "war-camp-chief", warCamp, 130, 1050,
            EnemyTheme.RuinOccupiers, EncounterDifficulty.Elite,
            new[] { EnemyType.GoblinChief, EnemyType.GoblinHunter, EnemyType.Goblin }, 4,
            true, false,
            (SpawnSocketRole.Elite, 670, -1, new[] { EnemyType.GoblinChief }),
            (SpawnSocketRole.GroundMelee, 390, -1, new[] { EnemyType.Goblin }),
            (SpawnSocketRole.Ranged, 1020, -72, new[] { EnemyType.GoblinHunter }),
            (SpawnSocketRole.GroundMelee, 1120, -1, new[] { EnemyType.Goblin }));

        WildForestSection nest = sections[6];
        AddEncounterZone(
            zones, sockets,
            "mothers-nest", nest, 330, 650,
            EnemyTheme.SpiderBrood, EncounterDifficulty.Elite,
            new[] { EnemyType.MotherSpider, EnemyType.GiantSpider, EnemyType.Spiderling }, 5,
            true, false,
            (SpawnSocketRole.Heavy, 735, -194, new[] { EnemyType.MotherSpider }),
            (SpawnSocketRole.Minion, 540, -142, new[] { EnemyType.Spiderling }),
            (SpawnSocketRole.Minion, 880, -194, new[] { EnemyType.Spiderling }),
            (SpawnSocketRole.Ambush, 975, -124, new[] { EnemyType.GiantSpider }));
        EncounterZone nestZone = zones[^1];
        zones[^1] = new EncounterZone(
            nestZone.ZoneId,
            nestZone.SectionId,
            new Rectangle(
                nest.Bounds.Left + 330,
                nest.Bounds.Top + 300,
                650,
                250),
            nestZone.GroundBounds,
            nestZone.Theme,
            nestZone.Difficulty,
            new[] { EnemyType.MotherSpider, EnemyType.GiantSpider, EnemyType.Spiderling },
            nestZone.MaxConcurrentEnemies,
            isEliteZone: true,
            isBossZone: false,
            allowRespawn: false,
            introEligible: true);

        WildForestSection arena = sections[7];
        AddEncounterZone(
            zones, sockets,
            "ancient-treant-arena", arena, 60, arena.Bounds.Width - 120,
            EnemyTheme.AncientTreant, EncounterDifficulty.Boss,
            Array.Empty<EnemyType>(), 1,
            false,
            true,
            (SpawnSocketRole.Boss, arena.Bounds.Width / 2, -1, Array.Empty<EnemyType>()));
    }

    private static List<WildForestRouteFeature> CreateWildForestRouteFeatures(
        IReadOnlyList<WildForestSection> sections)
    {
        WildForestSection outskirts = sections[0];
        WildForestSection webwood = sections[2];
        WildForestSection thorns = sections[3];
        WildForestSection grove = sections[4];
        WildForestSection warCamp = sections[5];
        WildForestSection nest = sections[6];
        return new List<WildForestRouteFeature>
        {
            Feature("outskirts-canopy", outskirts, WildForestRouteFeatureKind.SideBranch, 610, -150, 290, 126),
            Feature("webwood-upper", webwood, WildForestRouteFeatureKind.SideBranch, 160, -174, 860, 170),
            Feature("thorn-root-path", thorns, WildForestRouteFeatureKind.SideBranch, 330, -140, 540, 122),
            Feature("mothers-nest-optional", nest, WildForestRouteFeatureKind.SideBranch, 350, -230, 690, 224),
            Feature("grove-root-bridge", grove, WildForestRouteFeatureKind.Shortcut, 825, -126, 340, 116),
            Feature("war-camp-rampart", warCamp, WildForestRouteFeatureKind.Shortcut, 130, -112, 1040, 104),
            Feature("outskirts-cache", outskirts, WildForestRouteFeatureKind.Secret, 710, -78, 150, 74),
            Feature("webwood-cocoon-cache", webwood, WildForestRouteFeatureKind.Secret, 420, -205, 150, 76),
            Feature("thorn-hollow", thorns, WildForestRouteFeatureKind.Secret, 645, -126, 145, 70),
            Feature("war-camp-supply-cache", warCamp, WildForestRouteFeatureKind.Secret, 1020, -110, 150, 76)
        };
    }

    private static WildForestRouteFeature Feature(
        string id,
        WildForestSection section,
        WildForestRouteFeatureKind kind,
        int offsetX,
        int offsetY,
        int width,
        int height)
    {
        var bounds = new Rectangle(
            section.Bounds.Left + offsetX,
            section.GroundY + offsetY,
            width,
            height);
        return new WildForestRouteFeature(
            id,
            section.SectionId,
            kind,
            bounds,
            new Vector2(bounds.Left, bounds.Bottom),
            new Vector2(bounds.Right, bounds.Bottom));
    }

    private static List<WildForestSpawnProfile> CreateWildForestSpawnProfiles(
        IReadOnlyList<WildForestSection> sections)
    {
        return new List<WildForestSpawnProfile>
        {
            Profile(sections[0], new[] { EnemyType.DireWolf, EnemyType.BloodBat }, new[] { 80, 20 }, 2, 250f, true, true),
            Profile(sections[1], new[] { EnemyType.Goblin, EnemyType.GoblinHunter }, new[] { 70, 30 }, 4, 300f, true, false),
            Profile(sections[2], new[] { EnemyType.GiantSpider, EnemyType.Spiderling }, new[] { 75, 25 }, 4, 280f, true, false),
            Profile(sections[3], new[] { EnemyType.ThornCrawler }, new[] { 100 }, 3, 260f, true, false),
            Profile(sections[4], new[] { EnemyType.CorruptedTreant, EnemyType.BloodBat, EnemyType.ThornCrawler }, new[] { 45, 25, 30 }, 4, 330f, true, true, 420),
            Profile(sections[5], new[] { EnemyType.GoblinChief, EnemyType.GoblinHunter, EnemyType.Goblin }, new[] { 35, 25, 40 }, 4, 360f, true, false),
            Profile(sections[6], new[] { EnemyType.MotherSpider, EnemyType.GiantSpider, EnemyType.Spiderling }, new[] { 45, 35, 20 }, 5, 320f, true, false, 360),
            Profile(sections[7], Array.Empty<EnemyType>(), Array.Empty<int>(), 1, 480f, true, false, 700)
        };
    }

    private static WildForestSpawnProfile Profile(
        WildForestSection section,
        EnemyType[] types,
        int[] weights,
        int maximumConcurrent,
        float minimumPlayerDistance,
        bool requiresGround,
        bool allowsAerial,
        int minimumHeavyGroundWidth = 260)
    {
        return new WildForestSpawnProfile(
            $"{section.SectionId}-ecology",
            section.SectionId,
            types,
            weights,
            maximumConcurrent,
            minimumPlayerDistance,
            requiresGround,
            allowsAerial,
            minimumHeavyGroundWidth);
    }

    private static void AddEncounterZone(
        List<EncounterZone> zones,
        List<SpawnSocket> sockets,
        string zoneId,
        WildForestSection section,
        int offsetX,
        int width,
        EnemyTheme theme,
        EncounterDifficulty difficulty,
        EnemyType[] types,
        int maxConcurrent,
        params (SpawnSocketRole Role, int OffsetX, int OffsetY, EnemyType[] Types)[] socketData)
    {
        AddEncounterZone(
            zones, sockets, zoneId, section, offsetX, width, theme,
            difficulty, types, maxConcurrent, false, false, socketData);
    }

    private static void AddEncounterZone(
        List<EncounterZone> zones,
        List<SpawnSocket> sockets,
        string zoneId,
        WildForestSection section,
        int offsetX,
        int width,
        EnemyTheme theme,
        EncounterDifficulty difficulty,
        EnemyType[] types,
        int maxConcurrent,
        bool isElite = false,
        bool isBoss = false,
        params (SpawnSocketRole Role, int OffsetX, int OffsetY, EnemyType[] Types)[] socketData)
    {
        var activation = new Rectangle(
            section.Bounds.Left + offsetX,
            section.Bounds.Top + 90,
            width,
            section.Bounds.Height - 160);
        var ground = new Rectangle(
            activation.Left,
            section.GroundY - 92,
            activation.Width,
            92);
        zones.Add(new EncounterZone(
            zoneId,
            section.SectionId,
            activation,
            ground,
            theme,
            difficulty,
            types,
            maxConcurrent,
            isEliteZone: isElite,
            isBossZone: isBoss,
            allowRespawn: false,
            introEligible: true));

        for (int index = 0; index < socketData.Length; index++)
        {
            var data = socketData[index];
            float y = data.OffsetY < 0
                ? section.GroundY + data.OffsetY
                : section.Bounds.Top + data.OffsetY;
            Vector2 position = new(
                section.Bounds.Left + data.OffsetX,
                y);
            bool aerial = data.Role == SpawnSocketRole.Flying;
            Rectangle placement = new(
                (int)position.X - (aerial ? 70 : 46),
                (int)position.Y - (aerial ? 42 : 82),
                aerial ? 140 : 92,
                aerial ? 84 : 82);
            sockets.Add(new SpawnSocket(
                $"{zoneId}-{index + 1:00}",
                zoneId,
                data.Role,
                position,
                placement,
                data.Types,
                introEligible: true));
        }
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
