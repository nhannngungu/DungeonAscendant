using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

/// <summary>
/// Map 7 foundation only: authored terrain, presentation metadata, safe
/// checkpoints, and inert future encounter sockets. No combat content lives
/// in this generator.
/// </summary>
public sealed class DemonKingSanctumMapGenerator
{
    public const int ZoneCount = 10;
    public const int WorldWidth = 18908;
    public const int NeutralSocketCount = 10;
    public const int TotalSocketCount = NeutralSocketCount + 1;
    private const int Margin = 80;
    private const int TransitionWidth = 112;

    public DungeonMap Generate()
    {
        int[] widths =
            { 1500, 1900, 1800, 1900, 2200, 1800, 1900, 1700, 2300, 900 };
        int[] grounds =
            { 650, 658, 690, 704, 682, 710, 650, 612, 600, 604 };
        string[] keys =
        {
            "sanctum.zone.approach",
            "sanctum.zone.broken_vows",
            "sanctum.zone.pilgrims_descent",
            "sanctum.zone.altar_of_sacrifice",
            "sanctum.zone.ancient_kings",
            "sanctum.zone.abyssal_cloister",
            "sanctum.zone.throne_ascent",
            "sanctum.zone.royal_antechamber",
            "sanctum.zone.throne_sanctum",
            "sanctum.zone.final_endpoint"
        };
        DemonKingSanctumZoneKind[] kinds =
            (DemonKingSanctumZoneKind[])Enum.GetValues(
                typeof(DemonKingSanctumZoneKind));
        var rooms = new List<DungeonRoom>(ZoneCount);
        var corridors = new List<Rectangle>(ZoneCount - 1);
        var zones = new List<DemonKingSanctumZoneDefinition>(ZoneCount);
        int x = Margin;

        for (int index = 0; index < ZoneCount; index++)
        {
            RoomType type = index == 0 ? RoomType.Start :
                index == 4 ? RoomType.Treasure :
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
            zones.Add(new DemonKingSanctumZoneDefinition(
                $"sanctum-{index + 1:00}",
                keys[index],
                kinds[index],
                room.Bounds,
                room.GroundY));

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
                AddTransition(
                    previous,
                    corridor,
                    previous.GroundY,
                    room.GroundY);
            }
            x += widths[index] +
                (index < ZoneCount - 1 ? TransitionWidth : 0);
        }

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(Margin, Margin, WorldWidth, DungeonGenerator.RoomHeight),
            isDemonKingSanctum: true,
            encounterZones: CreateNeutralEncounterSpaces(zones),
            spawnSockets: CreateNeutralSockets(zones),
            demonKingSanctumZones: zones,
            sanctumProps: CreateProps(zones),
            sanctumCheckpoints: CreateCheckpoints(zones),
            authoredExitPosition: new Vector2(
                rooms[^1].Bounds.Right - 150f,
                rooms[^1].GroundY - 28f));
    }

    private static void AddGeometry(
        DungeonRoom room,
        DemonKingSanctumZoneKind kind)
    {
        int left = room.Bounds.Left;
        int ground = room.GroundY;
        Ground(room, left, room.Bounds.Width, ground);

        switch (kind)
        {
            case DemonKingSanctumZoneKind.SanctumApproach:
                Raised(room, left + 320, ground - 28, 240);
                Raised(room, left + 650, ground - 54, 250);
                Raised(room, left + 990, ground - 80, 260);
                break;
            case DemonKingSanctumZoneKind.HallOfBrokenVows:
                Raised(room, left + 390, ground - 34, 310);
                Raised(room, left + 1200, ground - 34, 310);
                break;
            case DemonKingSanctumZoneKind.PilgrimsDescent:
                Raised(room, left + 260, ground - 118, 290);
                Raised(room, left + 650, ground - 82, 300);
                Raised(room, left + 1050, ground - 48, 310);
                break;
            case DemonKingSanctumZoneKind.AltarOfSacrifice:
                Raised(room, left + 610, ground - 42, 680);
                break;
            case DemonKingSanctumZoneKind.HallOfAncientKings:
                Raised(room, left + 480, ground - 32, 260);
                Raised(room, left + 1460, ground - 32, 260);
                break;
            case DemonKingSanctumZoneKind.AbyssalCloister:
                Raised(room, left + 270, ground - 86, 340);
                Raised(room, left + 745, ground - 132, 310);
                Raised(room, left + 1190, ground - 82, 330);
                break;
            case DemonKingSanctumZoneKind.ThroneAscent:
                Raised(room, left + 260, ground - 30, 250);
                Raised(room, left + 600, ground - 62, 260);
                Raised(room, left + 950, ground - 96, 270);
                Raised(room, left + 1320, ground - 130, 280);
                break;
            case DemonKingSanctumZoneKind.RoyalAntechamber:
                Raised(room, left + 650, ground - 24, 400);
                break;
            case DemonKingSanctumZoneKind.ThroneSanctum:
            case DemonKingSanctumZoneKind.FinalEndpoint:
                // Reserved story spaces deliberately keep a clean, flat floor.
                break;
        }
    }

    private static List<EncounterZone> CreateNeutralEncounterSpaces(
        IReadOnlyList<DemonKingSanctumZoneDefinition> zones)
    {
        int[] zoneIndices = { 1, 2, 3, 4, 5, 6, 7, 8 };
        var spaces = new List<EncounterZone>(zoneIndices.Length);
        foreach (int index in zoneIndices)
        {
            DemonKingSanctumZoneDefinition zone = zones[index];
            spaces.Add(new EncounterZone(
                $"{zone.ZoneId}-future-encounter",
                zone.ZoneId,
                new Rectangle(
                    zone.Bounds.Left + 120,
                    zone.Bounds.Top + 55,
                    zone.Bounds.Width - 240,
                    zone.Bounds.Height - 90),
                new Rectangle(
                    zone.Bounds.Left + 120,
                    zone.GroundY - 180,
                    zone.Bounds.Width - 240,
                    180),
                EnemyTheme.None,
                EncounterDifficulty.Standard,
                Array.Empty<EnemyType>(),
                maxConcurrentEnemies: 0,
                isBossZone: index == 8,
                introEligible: false));
        }
        DemonKingSanctumZoneDefinition vows = zones[1];
        spaces[0] = new EncounterZone(
            "sanctum-broken-vows-encounter",
            vows.ZoneId,
            new Rectangle(vows.Bounds.Left + 120, vows.Bounds.Top + 55,
                vows.Bounds.Width - 240, vows.Bounds.Height - 90),
            new Rectangle(vows.Bounds.Left + 120, vows.GroundY - 180,
                vows.Bounds.Width - 240, 180),
            EnemyTheme.None,
            EncounterDifficulty.Standard,
            new[] { EnemyType.OathboundSentinel, EnemyType.VowshardAcolyte },
            maxConcurrentEnemies: 2,
            isBossZone: false,
            introEligible: true);
        DemonKingSanctumZoneDefinition descent = zones[2];
        spaces[1] = new EncounterZone(
            "sanctum-penitent-descent",
            descent.ZoneId,
            new Rectangle(descent.Bounds.Left + 120, descent.Bounds.Top + 55,
                descent.Bounds.Width - 240, descent.Bounds.Height - 90),
            new Rectangle(descent.Bounds.Left + 120, descent.GroundY - 210,
                descent.Bounds.Width - 240, 210),
            EnemyTheme.None, EncounterDifficulty.Standard,
            new[] { EnemyType.PenitentHusk }, 1,
            isBossZone: false, introEligible: true);
        DemonKingSanctumZoneDefinition altar = zones[3];
        spaces[2] = new EncounterZone(
            "sanctum-abyss-scribe-altar",
            altar.ZoneId,
            new Rectangle(altar.Bounds.Left + 120, altar.Bounds.Top + 55,
                altar.Bounds.Width - 240, altar.Bounds.Height - 90),
            new Rectangle(altar.Bounds.Left + 120, altar.GroundY - 190,
                altar.Bounds.Width - 240, 190),
            EnemyTheme.None, EncounterDifficulty.Standard,
            new[] { EnemyType.AbyssScribe }, 1,
            isBossZone: false, introEligible: true);
        DemonKingSanctumZoneDefinition kings = zones[4];
        spaces[3] = new EncounterZone(
            "sanctum-grave-reliquary-kings",
            kings.ZoneId,
            new Rectangle(kings.Bounds.Left + 120, kings.Bounds.Top + 55,
                kings.Bounds.Width - 240, kings.Bounds.Height - 90),
            new Rectangle(kings.Bounds.Left + 120, kings.GroundY - 190,
                kings.Bounds.Width - 240, 190),
            EnemyTheme.None, EncounterDifficulty.Standard,
            new[] { EnemyType.GraveReliquary }, 1,
            isBossZone: false, introEligible: true);
        DemonKingSanctumZoneDefinition cloister = zones[5];
        spaces[4] = new EncounterZone(
            "sanctum-cloister-wraith",
            cloister.ZoneId,
            new Rectangle(cloister.Bounds.Left + 120,
                cloister.Bounds.Top + 55,
                cloister.Bounds.Width - 240, cloister.Bounds.Height - 90),
            new Rectangle(cloister.Bounds.Left + 120,
                cloister.GroundY - 260,
                cloister.Bounds.Width - 240, 260),
            EnemyTheme.None, EncounterDifficulty.Standard,
            new[] { EnemyType.CloisterWraith }, 1,
            isBossZone: false, introEligible: true);
        for (int i = 6; i <= 7; i++)
        {
            DemonKingSanctumZoneDefinition zone = zones[i];
            EnemyType type = i == 6 ? EnemyType.RoyalSanctumKnight : EnemyType.AbyssOracle;
            spaces[i - 1] = new EncounterZone(
                i == 6 ? "sanctum-royal-knight-ascent" : "sanctum-abyss-oracle-antechamber",
                zone.ZoneId,
                new Rectangle(zone.Bounds.Left + 120, zone.Bounds.Top + 55,
                    zone.Bounds.Width - 240, zone.Bounds.Height - 90),
                new Rectangle(zone.Bounds.Left + 120, zone.GroundY - 220,
                    zone.Bounds.Width - 240, 220),
                EnemyTheme.None, EncounterDifficulty.Elite,
                new[] { type }, 1, isEliteZone: true, introEligible: true);
        }
        // Socket 10 is the guardian at the entrance, not the final sovereign.
        // Only the local pre-throne space is active; there is no traversal gate.
        DemonKingSanctumZoneDefinition throneEntrance = zones[8];
        spaces[7] = new EncounterZone(
            "sanctum-thronebound-seraph", throneEntrance.ZoneId,
            new Rectangle(throneEntrance.Bounds.Left + 100, throneEntrance.Bounds.Top + 55,
                1180, throneEntrance.Bounds.Height - 90),
            new Rectangle(throneEntrance.Bounds.Left + 100, throneEntrance.GroundY - 240, 1180, 240),
            EnemyTheme.None, EncounterDifficulty.Elite,
            new[] { EnemyType.ThroneboundSeraph }, 1, isEliteZone: true,
            isBossZone: false, introEligible: true);
        spaces.Add(new EncounterZone(VaeltharEnemy.EncounterId, throneEntrance.ZoneId,
            new Rectangle(throneEntrance.Bounds.Left + 1310, throneEntrance.Bounds.Top + 55,
                throneEntrance.Bounds.Width - 1430, throneEntrance.Bounds.Height - 90),
            new Rectangle(throneEntrance.Bounds.Left + 1310, throneEntrance.GroundY - 240,
                throneEntrance.Bounds.Width - 1430, 240),
            EnemyTheme.None, EncounterDifficulty.Boss, new[] { EnemyType.Vaelthar }, 1,
            isBossZone: true, introEligible: true));
        return spaces;
    }

    private static List<SpawnSocket> CreateNeutralSockets(
        IReadOnlyList<DemonKingSanctumZoneDefinition> zones)
    {
        var sockets = new List<SpawnSocket>(NeutralSocketCount);
        AddCombatSocket(sockets, zones[1], 1, 760,
            SpawnSocketRole.GroundMelee, EnemyType.OathboundSentinel);
        AddCombatSocket(sockets, zones[1], 2, 1370,
            SpawnSocketRole.Ranged, EnemyType.VowshardAcolyte);
        AddCombatSocket(sockets, zones[2], 3, 680,
            SpawnSocketRole.Heavy, EnemyType.PenitentHusk);
        AddCombatSocket(sockets, zones[3], 4, 420,
            SpawnSocketRole.Ranged, EnemyType.AbyssScribe);
        AddNeutralSocket(sockets, zones[3], 5, 1450);
        AddCombatSocket(sockets, zones[4], 6, 1020,
            SpawnSocketRole.Heavy, EnemyType.GraveReliquary);
        AddCombatSocket(sockets, zones[5], 7, 870,
            SpawnSocketRole.Flying, EnemyType.CloisterWraith);
        AddCombatSocket(sockets, zones[6], 8, 880,
            SpawnSocketRole.Elite, EnemyType.RoyalSanctumKnight);
        AddCombatSocket(sockets, zones[7], 9, 1180,
            SpawnSocketRole.Elite, EnemyType.AbyssOracle);
        AddCombatSocket(sockets, zones[8], 10, 620,
            SpawnSocketRole.Elite, EnemyType.ThroneboundSeraph);
        var throne = zones[8];
        var bossPosition = new Vector2(throne.Bounds.Left + 1740, throne.GroundY);
        sockets.Add(new SpawnSocket("map7-boss-vaelthar", VaeltharEnemy.EncounterId,
            SpawnSocketRole.Boss, bossPosition,
            new Rectangle((int)bossPosition.X - 130, (int)bossPosition.Y - 200, 260, 220),
            new[] { EnemyType.Vaelthar }, introEligible: true));
        return sockets;
    }

    private static void AddNeutralSocket(
        List<SpawnSocket> sockets,
        DemonKingSanctumZoneDefinition zone,
        int sequence,
        int offsetX)
    {
        var position = new Vector2(zone.Bounds.Left + offsetX, zone.GroundY);
        sockets.Add(new SpawnSocket(
            $"map7-neutral-{sequence:00}",
            zone.ZoneId,
            SpawnSocketRole.Unassigned,
            position,
            new Rectangle(
                (int)position.X - 120,
                (int)position.Y - 240,
                240,
                260),
            Array.Empty<EnemyType>(),
            introEligible: false));
    }

    private static void AddCombatSocket(
        List<SpawnSocket> sockets,
        DemonKingSanctumZoneDefinition zone,
        int sequence,
        int offsetX,
        SpawnSocketRole role,
        EnemyType type)
    {
        float footY = type switch
        {
            EnemyType.VowshardAcolyte => zone.GroundY - 34f,
            EnemyType.PenitentHusk => zone.GroundY - 82f,
            // The Wraith hovers just above the gameplay plane so its claw and
            // local control kit remain readable and reachable.  It is aerial,
            // but it is not a high-altitude turret.
            EnemyType.CloisterWraith => zone.GroundY - 62f,
            _ => zone.GroundY
        };
        var position = new Vector2(zone.Bounds.Left + offsetX, footY);
        string encounterId = type switch
        {
            EnemyType.PenitentHusk => "sanctum-penitent-descent",
            EnemyType.AbyssScribe => "sanctum-abyss-scribe-altar",
            EnemyType.GraveReliquary => "sanctum-grave-reliquary-kings",
            EnemyType.CloisterWraith => "sanctum-cloister-wraith",
            EnemyType.RoyalSanctumKnight => "sanctum-royal-knight-ascent",
            EnemyType.AbyssOracle => "sanctum-abyss-oracle-antechamber",
            EnemyType.ThroneboundSeraph => "sanctum-thronebound-seraph",
            _ => "sanctum-broken-vows-encounter"
        };
        sockets.Add(new SpawnSocket(
            $"map7-enemy-{sequence:00}",
            encounterId,
            role,
            position,
            new Rectangle((int)position.X - 130,
                (int)position.Y - 180, 260, 200),
            new[] { type },
            introEligible: true));
    }

    private static List<SanctumCheckpointDefinition> CreateCheckpoints(
        IReadOnlyList<DemonKingSanctumZoneDefinition> zones) => new()
    {
        Checkpoint("sanctum-entry", zones[0], 210),
        Checkpoint("descent-complete", zones[3], 180),
        Checkpoint("ancient-kings", zones[4], 220),
        Checkpoint("before-throne-ascent", zones[5], 1580),
        Checkpoint("royal-antechamber", zones[7], 230)
    };

    private static SanctumCheckpointDefinition Checkpoint(
        string id,
        DemonKingSanctumZoneDefinition zone,
        int offsetX) => new(
            id,
            zone.ZoneId,
            new Vector2(zone.Bounds.Left + offsetX, zone.GroundY - 28f));

    private static List<SanctumPropDefinition> CreateProps(
        IReadOnlyList<DemonKingSanctumZoneDefinition> z)
    {
        var props = new List<SanctumPropDefinition>();
        void Add(int zone, string id, SanctumPropKind kind,
            int x, int y, int width, int height,
            SanctumLayer layer = SanctumLayer.Midground) =>
            props.Add(new SanctumPropDefinition(
                id,
                z[zone].ZoneId,
                kind,
                new Rectangle(
                    z[zone].Bounds.Left + x,
                    z[zone].GroundY + y,
                    width,
                    height),
                layer));

        Add(0, "approach-buried-wall", SanctumPropKind.BuriedWall,
            40, -450, 920, 450, SanctumLayer.Background);
        Add(0, "approach-sealed-arch", SanctumPropKind.SealedArch,
            760, -490, 610, 490);
        Add(0, "approach-statue", SanctumPropKind.RoyalStatue,
            170, -290, 150, 290);
        Add(0, "approach-braziers", SanctumPropKind.Brazier,
            1220, -120, 90, 120, SanctumLayer.Foreground);

        for (int i = 0; i < 5; i++)
            Add(1, $"vow-column-{i}", SanctumPropKind.MonumentalColumn,
                90 + i * 390, -500, 105, 500,
                i % 2 == 0 ? SanctumLayer.Background : SanctumLayer.Midground);
        Add(1, "fractured-oath-tablets", SanctumPropKind.OathTablet,
            300, -220, 1250, 190);
        Add(1, "broken-vow-banners", SanctumPropKind.BrokenBanner,
            520, -440, 850, 260, SanctumLayer.Foreground);

        Add(2, "descent-shaft", SanctumPropKind.VoidWindow,
            520, -520, 760, 430, SanctumLayer.Background);
        Add(2, "descent-terrace-a", SanctumPropKind.Terrace,
            130, -280, 540, 85);
        Add(2, "descent-terrace-b", SanctumPropKind.Terrace,
            1010, -190, 560, 80);
        Add(2, "descent-broken-bridge", SanctumPropKind.BrokenBridge,
            610, -360, 650, 90, SanctumLayer.Background);

        Add(3, "sacrifice-altar", SanctumPropKind.RitualAltar,
            610, -240, 680, 240);
        Add(3, "sacrifice-channel", SanctumPropKind.RitualChannel,
            190, -22, 1520, 22);
        Add(3, "sacrifice-chains-left", SanctumPropKind.SuspendedChain,
            300, -500, 70, 390, SanctumLayer.Foreground);
        Add(3, "sacrifice-chains-right", SanctumPropKind.SuspendedChain,
            1510, -520, 70, 410, SanctumLayer.Foreground);
        Add(3, "ancient-vessels", SanctumPropKind.AncientVessel,
            220, -150, 1420, 150);

        for (int i = 0; i < 5; i++)
        {
            Add(4, $"king-alcove-{i}", SanctumPropKind.KingAlcove,
                70 + i * 430, -520, 350, 500, SanctumLayer.Background);
            Add(4, $"king-statue-{i}", SanctumPropKind.RoyalStatue,
                155 + i * 430, -390 + (i % 2) * 22, 170, 370);
        }
        Add(4, "broken-royal-crowns", SanctumPropKind.BrokenCrown,
            170, -60, 1840, 60, SanctumLayer.Foreground);

        Add(5, "cloister-void", SanctumPropKind.VoidWindow,
            130, -540, 1540, 500, SanctumLayer.Background);
        Add(5, "cloister-floating-stone", SanctumPropKind.FloatingStone,
            260, -420, 1260, 310, SanctumLayer.Background);
        Add(5, "cloister-bridge", SanctumPropKind.NarrowBridge,
            300, -250, 1180, 70);
        Add(5, "cloister-fragments", SanctumPropKind.SacredFragment,
            170, -470, 1450, 380, SanctumLayer.Foreground);

        Add(6, "ascent-royal-gate", SanctumPropKind.RoyalGate,
            1160, -520, 620, 520);
        Add(6, "ascent-throne-shadow", SanctumPropKind.ThroneSilhouette,
            690, -500, 520, 430, SanctumLayer.Background);
        for (int i = 0; i < 4; i++)
            Add(6, $"ascent-column-{i}", SanctumPropKind.MonumentalColumn,
                90 + i * 470, -470, 110, 470);

        Add(7, "antechamber-door", SanctumPropKind.RoyalGate,
            1110, -520, 500, 520);
        Add(7, "antechamber-statues", SanctumPropKind.RoyalStatue,
            170, -350, 1280, 350, SanctumLayer.Background);
        Add(7, "antechamber-polish", SanctumPropKind.RitualChannel,
            130, -14, 1440, 14);

        Add(8, "throne-void", SanctumPropKind.VoidWindow,
            250, -540, 1800, 500, SanctumLayer.Background);
        Add(8, "demon-kings-throne", SanctumPropKind.Throne,
            745, -520, 810, 520);
        Add(8, "throne-column-left", SanctumPropKind.MonumentalColumn,
            110, -520, 150, 520);
        Add(8, "throne-column-right", SanctumPropKind.MonumentalColumn,
            2040, -520, 150, 520);
        Add(8, "throne-sacred-fragments", SanctumPropKind.SacredFragment,
            250, -500, 1800, 430, SanctumLayer.Foreground);

        Add(9, "endpoint-arch", SanctumPropKind.SealedArch,
            150, -480, 600, 480);
        Add(9, "endpoint-fragments", SanctumPropKind.SacredFragment,
            90, -390, 720, 340, SanctumLayer.Background);
        return props;
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
        int quarter = corridor.Width / 4;
        for (int step = 0; step < 4; step++)
        {
            int y = (int)MathF.Round(MathHelper.Lerp(
                fromGround,
                toGround,
                (step + 1f) / 5f));
            int width = step == 3
                ? corridor.Width - quarter * 3
                : quarter;
            Ground(room, corridor.Left + quarter * step, width, y);
        }
    }
}
