using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

/// <summary>
/// Authored Map 5 foundation. The route and sockets are intentionally stable so
/// later formation, alert-state, and boss passes can build on them.
/// </summary>
public sealed class DemonFortressMapGenerator
{
    public const int ZoneCount = 9;
    public const int WorldWidth = 17146;
    private const int Margin = 80;
    private const int TransitionWidth = 112;

    public DungeonMap Generate()
    {
        int[] widths = { 1750, 1600, 2300, 2100, 2100, 1900, 1700, 1900, 900 };
        int[] ground = { 684, 680, 676, 682, 630, 590, 604, 612, 616 };
        string[] keys =
        {
            "fortress.zone.outer_battlefield", "fortress.zone.demon_gate",
            "fortress.zone.barracks", "fortress.zone.infernal_forge",
            "fortress.zone.citadel_ramparts", "fortress.zone.command_citadel",
            "fortress.zone.ballista_bastion", "fortress.zone.execution_court",
            "fortress.zone.sealed_ascent"
        };
        DemonFortressZoneKind[] kinds =
            (DemonFortressZoneKind[])Enum.GetValues(typeof(DemonFortressZoneKind));
        var rooms = new List<DungeonRoom>(ZoneCount);
        var corridors = new List<Rectangle>(ZoneCount - 1);
        var zones = new List<DemonFortressZoneDefinition>(ZoneCount);
        int x = Margin;

        for (int index = 0; index < ZoneCount; index++)
        {
            RoomType type = index == 0 ? RoomType.Start :
                index == 3 ? RoomType.Treasure :
                index == 7 ? RoomType.Boss :
                index == 8 ? RoomType.Exit : RoomType.Enemy;
            var room = new DungeonRoom(
                index,
                new Rectangle(x, Margin, widths[index], DungeonGenerator.RoomHeight),
                type)
            {
                GroundY = ground[index]
            };
            AddGeometry(room, kinds[index]);
            rooms.Add(room);
            zones.Add(new DemonFortressZoneDefinition(
                $"fortress-{index + 1:00}", keys[index], kinds[index],
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

        var encounters = new List<EncounterZone>();
        var sockets = new List<SpawnSocket>();
        AddEncounters(zones, encounters, sockets);

        return new DungeonMap(
            rooms,
            corridors,
            new Rectangle(Margin, Margin, WorldWidth, DungeonGenerator.RoomHeight),
            isDemonFortress: true,
            encounterZones: encounters,
            spawnSockets: sockets,
            spawnProfiles: CreateProfiles(zones),
            demonFortressZones: zones,
            fortressProps: CreateProps(zones),
            fortressCheckpoints: CreateCheckpoints(zones),
            authoredExitPosition: new Vector2(
                rooms[^1].Bounds.Right - 130, rooms[^1].GroundY - 28));
    }

    private static void AddGeometry(DungeonRoom room, DemonFortressZoneKind kind)
    {
        int left = room.Bounds.Left;
        int ground = room.GroundY;
        Ground(room, left, room.Bounds.Width, ground);

        switch (kind)
        {
            case DemonFortressZoneKind.OuterBattlefield:
                Raised(room, left + 410, ground - 34, 180);
                Raised(room, left + 1030, ground - 42, 210);
                break;
            case DemonFortressZoneKind.DemonGate:
                Raised(room, left + 250, ground - 66, 260);
                Raised(room, left + 1080, ground - 66, 260);
                break;
            case DemonFortressZoneKind.Barracks:
                Raised(room, left + 380, ground - 70, 330);
                Raised(room, left + 1480, ground - 82, 340);
                break;
            case DemonFortressZoneKind.InfernalForge:
                Raised(room, left + 320, ground - 58, 300);
                Raised(room, left + 850, ground - 112, 350);
                Raised(room, left + 1450, ground - 62, 300);
                break;
            case DemonFortressZoneKind.CitadelRamparts:
                Raised(room, left + 360, ground - 48, 300);
                Raised(room, left + 1120, ground - 72, 360);
                break;
            case DemonFortressZoneKind.CommandCitadel:
                Raised(room, left + 300, ground - 58, 260);
                Raised(room, left + 1340, ground - 58, 260);
                break;
            // Boss-tier arenas and sealed ascent deliberately remain clean.
            case DemonFortressZoneKind.BallistaBastion:
            case DemonFortressZoneKind.ExecutionCourt:
            case DemonFortressZoneKind.SealedAscent:
                break;
        }
    }

    private static void AddEncounters(
        IReadOnlyList<DemonFortressZoneDefinition> zones,
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets)
    {
        Add(encounters, sockets, zones[0], "fortress-frontline", 180, 1320,
            EnemyTheme.FortressBattlefield, EncounterDifficulty.Standard, 3,
            (SpawnSocketRole.GroundMelee, 620, EnemyType.DemonLegionnaireFortress),
            (SpawnSocketRole.Ranged, 980, EnemyType.FortressArcher),
            (SpawnSocketRole.GroundMelee, 1390, EnemyType.DemonLegionnaireFortress));
        Add(encounters, sockets, zones[1], "fortress-gate-defense", 150, 1260,
            EnemyTheme.FortressGate, EncounterDifficulty.Hard, 3,
            (SpawnSocketRole.Heavy, 570, EnemyType.DemonShieldbearer),
            (SpawnSocketRole.Ranged, 870, EnemyType.FortressArcher),
            (SpawnSocketRole.Elite, 1375, EnemyType.HornGuard));
        Add(encounters, sockets, zones[2], "fortress-barracks-drill", 180, 1800,
            EnemyTheme.FortressBarracks, EncounterDifficulty.Hard, 4,
            (SpawnSocketRole.GroundMelee, 760, EnemyType.DemonLegionnaireFortress),
            (SpawnSocketRole.GroundMelee, 980, EnemyType.WarHound),
            (SpawnSocketRole.Elite, 1370, EnemyType.HornGuard),
            (SpawnSocketRole.GroundMelee, 1900, EnemyType.WarHound));
        Add(encounters, sockets, zones[3], "fortress-forge-watch", 170, 1690,
            EnemyTheme.FortressForge, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Heavy, 690, EnemyType.InfernalSmith),
            (SpawnSocketRole.GroundMelee, 1280, EnemyType.InfernalPikeman),
            (SpawnSocketRole.Heavy, 1820, EnemyType.InfernalSmith));
        Add(encounters, sockets, zones[4], "fortress-rampart-line", 170, 1690,
            EnemyTheme.FortressRamparts, EncounterDifficulty.Elite, 4,
            (SpawnSocketRole.Ranged, 720, EnemyType.FortressArcher),
            (SpawnSocketRole.GroundMelee, 880, EnemyType.InfernalPikeman),
            (SpawnSocketRole.Heavy, 1540, EnemyType.DemonShieldbearer),
            (SpawnSocketRole.Ranged, 1600, EnemyType.FortressArcher));
        Add(encounters, sockets, zones[5], "fortress-command-guard", 170, 1510,
            EnemyTheme.FortressCommand, EncounterDifficulty.Elite, 3,
            (SpawnSocketRole.Heavy, 650, EnemyType.DemonShieldbearer),
            (SpawnSocketRole.Elite, 960, EnemyType.DemonOfficer),
            (SpawnSocketRole.GroundMelee, 1200, EnemyType.InfernalPikeman));
        Add(encounters, sockets, zones[6], "fortress-ballista-gunner", 150, 1400,
            EnemyTheme.FortressBallista, EncounterDifficulty.Boss, 1,
            (SpawnSocketRole.Boss, 880, EnemyType.BallistaGunner));
        Add(encounters, sockets, zones[7], "fortress-black-guard", 150, 1600,
            EnemyTheme.FortressExecution, EncounterDifficulty.Boss, 1,
            (SpawnSocketRole.Boss, 980, EnemyType.BlackGuardExecutioner));
    }

    private static void Add(
        List<EncounterZone> encounters,
        List<SpawnSocket> sockets,
        DemonFortressZoneDefinition zone,
        string id,
        int offsetX,
        int width,
        EnemyTheme theme,
        EncounterDifficulty difficulty,
        int cap,
        params (SpawnSocketRole role, int x, EnemyType type)[] data)
    {
        var activation = new Rectangle(
            zone.Bounds.Left + offsetX, zone.Bounds.Top + 80,
            width, zone.Bounds.Height - 140);
        encounters.Add(new EncounterZone(
            id, zone.ZoneId, activation,
            new Rectangle(activation.Left, zone.GroundY - 120, activation.Width, 120),
            theme, difficulty, Array.ConvertAll(data, item => item.type), cap,
            difficulty is EncounterDifficulty.Elite or EncounterDifficulty.Boss,
            difficulty == EncounterDifficulty.Boss,
            false, true));

        for (int index = 0; index < data.Length; index++)
        {
            var position = new Vector2(zone.Bounds.Left + data[index].x, zone.GroundY);
            sockets.Add(new SpawnSocket(
                $"{id}-{index + 1:00}", id, data[index].role, position,
                new Rectangle((int)position.X - 100, (int)position.Y - 130, 200, 160),
                new[] { data[index].type }, true));
        }
    }

    private static List<WildForestSpawnProfile> CreateProfiles(
        IReadOnlyList<DemonFortressZoneDefinition> zones)
    {
        EnemyType[][] roster =
        {
            new[] { EnemyType.DemonLegionnaireFortress, EnemyType.FortressArcher },
            new[] { EnemyType.DemonShieldbearer, EnemyType.FortressArcher, EnemyType.HornGuard },
            new[] { EnemyType.DemonLegionnaireFortress, EnemyType.WarHound, EnemyType.HornGuard },
            new[] { EnemyType.InfernalSmith, EnemyType.InfernalPikeman },
            new[] { EnemyType.FortressArcher, EnemyType.InfernalPikeman, EnemyType.DemonShieldbearer },
            new[] { EnemyType.DemonOfficer, EnemyType.DemonShieldbearer, EnemyType.InfernalPikeman },
            new[] { EnemyType.BallistaGunner },
            new[] { EnemyType.BlackGuardExecutioner },
            Array.Empty<EnemyType>()
        };
        var result = new List<WildForestSpawnProfile>(ZoneCount);
        for (int index = 0; index < ZoneCount; index++)
        {
            EnemyType[] types = roster[index].Length == 0
                ? new[] { EnemyType.DemonLegionnaireFortress }
                : roster[index];
            int[] weights = new int[types.Length];
            for (int item = 0; item < weights.Length; item++)
                weights[item] = 100 / weights.Length;
            weights[0] += 100 - Sum(weights);
            result.Add(new WildForestSpawnProfile(
                $"{zones[index].ZoneId}-profile", zones[index].ZoneId,
                types, weights, index >= 6 ? 1 : 4, 280f, true, false,
                index >= 3 ? 420 : 280));
        }
        return result;
    }

    private static List<FortressCheckpointDefinition> CreateCheckpoints(
        IReadOnlyList<DemonFortressZoneDefinition> zones) => new()
    {
        Checkpoint("gate-secured", zones[1], 1360),
        Checkpoint("barracks-cleared", zones[2], 2050),
        Checkpoint("forge-crossed", zones[4], 180),
        Checkpoint("command-approach", zones[5], 180),
        Checkpoint("boss-threshold", zones[6], 180)
    };

    private static FortressCheckpointDefinition Checkpoint(
        string id, DemonFortressZoneDefinition zone, int x) => new(
            id, zone.ZoneId, new Vector2(zone.Bounds.Left + x, zone.GroundY - 28));

    private static List<FortressPropDefinition> CreateProps(
        IReadOnlyList<DemonFortressZoneDefinition> z)
    {
        var props = new List<FortressPropDefinition>();
        void Add(int zone, string id, FortressPropKind kind, int x, int y,
            int width, int height, FortressLayer layer = FortressLayer.Midground) =>
            props.Add(new FortressPropDefinition(
                id, z[zone].ZoneId, kind,
                new Rectangle(z[zone].Bounds.Left + x, z[zone].GroundY + y, width, height), layer));

        Add(0, "battle-trench-a", FortressPropKind.Trench, 260, -18, 300, 26, FortressLayer.Foreground);
        Add(0, "battle-siege", FortressPropKind.BrokenSiegeEngine, 720, -115, 210, 115);
        Add(0, "battle-barricade", FortressPropKind.Barricade, 1230, -72, 190, 72);
        Add(0, "battle-banner", FortressPropKind.DamagedBanner, 1510, -180, 62, 180);
        Add(1, "gate-tower-left", FortressPropKind.GateTower, 120, -430, 260, 430, FortressLayer.Background);
        Add(1, "gate-tower-right", FortressPropKind.GateTower, 1220, -430, 260, 430, FortressLayer.Background);
        Add(1, "gate-chain", FortressPropKind.Chain, 610, -370, 70, 300);
        Add(1, "gate-winch", FortressPropKind.GateMechanism, 900, -120, 180, 120);
        Add(2, "barracks-bunks", FortressPropKind.Bunk, 230, -140, 320, 140);
        Add(2, "barracks-racks", FortressPropKind.WeaponRack, 670, -125, 210, 125);
        Add(2, "barracks-training", FortressPropKind.TrainingDummy, 1080, -130, 90, 130);
        Add(2, "barracks-storage", FortressPropKind.Crate, 1450, -88, 260, 88);
        Add(2, "barracks-officer", FortressPropKind.OfficerTable, 1810, -105, 250, 105);
        Add(2, "barracks-war-bell", FortressPropKind.WarBell, 2130, -230, 110, 230, FortressLayer.Foreground);
        Add(3, "forge-furnace-a", FortressPropKind.Furnace, 180, -250, 260, 250, FortressLayer.Background);
        Add(3, "forge-anvil", FortressPropKind.Anvil, 690, -85, 130, 85);
        Add(3, "forge-channel", FortressPropKind.MoltenChannel, 1180, -14, 350, 22, FortressLayer.Foreground);
        Add(3, "forge-machine", FortressPropKind.ForgeMachine, 1650, -240, 270, 240, FortressLayer.Background);
        Add(3, "forge-metal", FortressPropKind.SuspendedMetal, 1030, -310, 180, 120);
        Add(4, "rampart-battlements", FortressPropKind.Battlement, 120, -165, 1860, 72, FortressLayer.Background);
        Add(4, "rampart-ballista", FortressPropKind.Ballista, 820, -125, 230, 125);
        Add(4, "rampart-cover", FortressPropKind.Cover, 1580, -92, 220, 92);
        Add(5, "command-pillars-left", FortressPropKind.Pillar, 170, -390, 120, 390, FortressLayer.Background);
        Add(5, "command-war-table", FortressPropKind.WarTable, 770, -100, 360, 100);
        Add(5, "command-pillars-right", FortressPropKind.Pillar, 1610, -390, 120, 390, FortressLayer.Background);
        Add(5, "command-banner", FortressPropKind.CommandBanner, 890, -420, 120, 250, FortressLayer.Background);
        Add(6, "ballista-emplacement", FortressPropKind.Ballista, 690, -145, 330, 145);
        Add(6, "ballista-cover-left", FortressPropKind.Cover, 270, -90, 210, 90);
        Add(6, "ballista-cover-right", FortressPropKind.Cover, 1240, -90, 210, 90);
        Add(7, "execution-pillars-left", FortressPropKind.Pillar, 180, -360, 125, 360, FortressLayer.Background);
        Add(7, "execution-trophies", FortressPropKind.TrophyWeapons, 810, -250, 280, 190, FortressLayer.Background);
        Add(7, "execution-pillars-right", FortressPropKind.Pillar, 1595, -360, 125, 360, FortressLayer.Background);
        Add(8, "map-six-sealed-gate", FortressPropKind.SealedGate, 610, -430, 250, 430, FortressLayer.Midground);
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
        DungeonRoom room, Rectangle corridor, int fromGround, int toGround)
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

    private static int Sum(int[] values)
    {
        int result = 0;
        foreach (int value in values)
            result += value;
        return result;
    }
}
