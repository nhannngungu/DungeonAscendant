using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonAscendant.Audio;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Dungeon;

public static class DemonCityMapValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonCityMapGenerator().Generate();
        Need(map.IsDemonCity && map.DemonCityZones.Count ==
            DemonCityMapGenerator.ZoneCount,
            "Map 6 must contain its ten planned city sectors.");
        Need(map.WorldBounds.Width == DemonCityMapGenerator.WorldWidth &&
            map.WorldBounds.Width > DemonFortressMapGenerator.WorldWidth &&
            map.WorldBounds.Width < 20000,
            "Map 6 must be larger than Map 5 without becoming a 20k hallway.");
        Need(map.StartRoom != null && map.TreasureRoom != null &&
            map.BossRoom != null && map.ExitRoom != null &&
            map.BossRoom != map.ExitRoom,
            "Map 6 room roles are incomplete.");
        Need(map.DemonCityProps.Count >= 40 &&
            map.DemonCityCheckpoints.Count == 5 &&
            map.DemonCityEventSpaces.Count == 8 &&
            map.EncounterZones.Count == 9,
            "Map 6 environment, checkpoint, or event metadata is incomplete.");
        Need(RegionDefinition.Get(RegionType.DemonCity).IsImplemented,
            "Map 6 is not registered as an implemented region.");

        ValidateConnections(map);
        ValidateContinuousRoute(map);
        ValidatePlayerTraversal(map);
        ValidateCheckpoints(map);
        ValidateEventSpaces(map);
        ValidateDeferredSockets(map);
        ValidateImplementedEnemyPasses(map);
        ValidateFinalResources();
        ValidateMalachar(map);
        ValidateIndustrialRitualSystems(map);
        ValidateBossArenaAndExit(map);
        ValidateEncounterTraversalIndependence(map);
        ValidateSessionAndMapFiveTransition();
        ValidateCheckpointLoad(map);
    }

    private static void ValidateConnections(DungeonMap map)
    {
        for (int index = 0; index < map.Rooms.Count - 1; index++)
        {
            Need(map.Rooms[index].Connections.Contains(map.Rooms[index + 1].Id) &&
                map.Rooms[index + 1].Connections.Contains(map.Rooms[index].Id),
                $"Map 6 rooms {index} and {index + 1} are not connected both ways.");
        }
    }

    private static void ValidateContinuousRoute(DungeonMap map)
    {
        int prior = int.MaxValue;
        for (int x = map.WorldBounds.Left + 4;
            x < map.WorldBounds.Right - 4; x += 8)
        {
            int top = MandatoryGroundTop(map, x);
            Need(top != int.MaxValue,
                $"Map 6 main route has a gap near X={x}.");
            if (prior != int.MaxValue)
            {
                Need(Math.Abs(top - prior) <= 24,
                    $"Map 6 main route has an unsafe step near X={x}.");
            }
            prior = top;
        }
    }

    private static void ValidatePlayerTraversal(DungeonMap map)
    {
        Vector2 size = new(
            DungeonAscendant.Player.Player.BodyHurtboxWidth,
            DungeonAscendant.Player.Player.BodyHurtboxHeight);
        Vector2 start = SideScrollingCollision.ResolveSafeEntrySpawn(
            map.StartRoom.Bounds.Left + 105f, size, map, 0f);
        var player = new DungeonAscendant.Player.Player(start);
        float target = map.ExitRoom.Bounds.Right - 155f;
        float priorX = player.Position.X;
        int stillFrames = 0;

        for (int frame = 0; frame < 12000 && player.Position.X < target; frame++)
        {
            bool jump = stillFrames >= 8 && player.IsGrounded;
            player.UpdateSideScrollingMovement(
                Frame, new KeyboardState(Keys.D), jump, map);
            stillFrames = player.Position.X <= priorX + .05f
                ? stillFrames + 1 : 0;
            priorX = player.Position.X;
        }

        Need(player.Position.X >= target,
            "Player A/D + Space simulation could not traverse Map 6.");
        Need(map.WorldBounds.Contains(player.Bounds),
            "Map 6 traversal left the world bounds.");
    }

    private static void ValidateCheckpoints(DungeonMap map)
    {
        Vector2 size = new(
            DungeonAscendant.Player.Player.BodyHurtboxWidth,
            DungeonAscendant.Player.Player.BodyHurtboxHeight);
        var ids = new HashSet<string>();
        foreach (DemonCityCheckpointDefinition checkpoint in
            map.DemonCityCheckpoints)
        {
            Need(ids.Add(checkpoint.CheckpointId),
                $"Duplicate Map 6 checkpoint {checkpoint.CheckpointId}.");
            DemonCityZoneDefinition zone = FindZone(map, checkpoint.ZoneId);
            Need(zone != null && zone.Bounds.Contains(checkpoint.Position.ToPoint()),
                $"{checkpoint.CheckpointId} is outside its city sector.");
            Need(SideScrollingCollision.IsPositionFree(
                    checkpoint.Position, size, map) &&
                SideScrollingCollision.IsSupported(
                    checkpoint.Position, size, map),
                $"{checkpoint.CheckpointId} is not a safe checkpoint position.");
            foreach (SpawnSocket socket in map.SpawnSockets)
            {
                Need(Vector2.DistanceSquared(
                        checkpoint.Position, socket.Position) >= 240f * 240f,
                    $"{checkpoint.CheckpointId} is too close to {socket.SocketId}.");
            }
        }
    }

    private static void ValidateEventSpaces(DungeonMap map)
    {
        var ids = new HashSet<string>();
        var kinds = new HashSet<DemonCityEventSpaceKind>();
        foreach (DemonCityEventSpaceDefinition space in map.DemonCityEventSpaces)
        {
            Need(ids.Add(space.EventId),
                $"Duplicate Map 6 event space {space.EventId}.");
            DemonCityZoneDefinition zone = FindZone(map, space.ZoneId);
            Need(zone != null && zone.Bounds.Contains(space.Bounds),
                $"{space.EventId} is outside its planned city sector.");
            kinds.Add(space.Kind);
        }
        Need(kinds.Contains(DemonCityEventSpaceKind.RitualChamber) &&
            kinds.Contains(DemonCityEventSpaceKind.ElevatedBridge) &&
            kinds.Contains(DemonCityEventSpaceKind.EliteCourtyard) &&
            kinds.Contains(DemonCityEventSpaceKind.PalaceHall) &&
            kinds.Contains(DemonCityEventSpaceKind.BossArena),
            "Map 6 event spaces do not cover its distinct district progression.");
    }

    private static void ValidateDeferredSockets(DungeonMap map)
    {
        Need(map.SpawnSockets.Count == 11,
            "Map 6 must reserve ten enemy sockets and one boss socket.");
        var ids = new HashSet<string>();
        int plannedEnemySockets = 0;
        int bossSockets = 0;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            Need(ids.Add(socket.SocketId),
                $"Duplicate Map 6 socket {socket.SocketId}.");
            DemonCityZoneDefinition zone = FindZone(map, socket.ZoneId);
            Need(zone != null && zone.Bounds.Contains(socket.Position.ToPoint()) &&
                zone.Bounds.Intersects(socket.PlacementBounds),
                $"{socket.SocketId} is outside its planned sector.");
            if (socket.SocketId == "map6-enemy-51-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.GroundMelee &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.DemonCityEnforcer }) &&
                    socket.IntroEligible,
                    "Enemy 51 socket is not assigned to the city enforcer.");
            }
            else if (socket.SocketId == "map6-enemy-52-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Ranged &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.SoulLanternKeeper }) &&
                    socket.IntroEligible,
                    "Enemy 52 socket is not assigned to the lantern keeper.");
            }
            else if (socket.SocketId == "map6-enemy-53-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.GroundMelee &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.AshbladeStalker }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.LowerDistrict,
                    "Enemy 53 socket is not assigned to the Lower District stalker.");
            }
            else if (socket.SocketId == "map6-enemy-54-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Ranged &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.ChainCollector }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.MarketDepths,
                    "Enemy 54 socket is not assigned to the Market Depths collector.");
            }
            else if (socket.SocketId == "map6-enemy-55-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Heavy &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.BloodworksServitor }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.IndustrialRitualDistrict,
                    "Enemy 55 socket is not assigned to the industrial servitor.");
            }
            else if (socket.SocketId == "map6-enemy-56-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Ranged &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.SigilCantor }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.IndustrialRitualDistrict,
                    "Enemy 56 socket is not assigned to the ritual cantor.");
            }
            else if (socket.SocketId == "map6-enemy-57-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Elite &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.VeilWarden }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.HighCity,
                    "Enemy 57 socket is not assigned to the High City Warden.");
            }
            else if (socket.SocketId == "map6-enemy-58-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Elite &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.GloomwingHarrier }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.EliteDistrict,
                    "Enemy 58 socket is not assigned to the Elite District Harrier.");
                DungeonRoom harrierRoom = map.FindRoomContaining(socket.Position);
                var flightClearance = new Rectangle(
                    (int)socket.Position.X - 70,
                    (int)socket.Position.Y - 210,
                    140,
                    185);
                Need(harrierRoom != null &&
                    !harrierRoom.Platforms.Any(platform =>
                        platform.Bounds.Intersects(flightClearance)),
                    "Enemy 58 socket lacks readable vertical flight clearance.");
            }
            else if (socket.SocketId == "map6-enemy-59-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Elite &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.CathedralButcher }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.EliteDistrict,
                    "Enemy 59 socket is not assigned to the Cathedral Butcher.");
            }
            else if (socket.SocketId == "map6-enemy-60-placeholder")
            {
                Need(socket.Role == SpawnSocketRole.Boss &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.AbyssalMagistrate }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.PalaceApproach,
                    "Enemy 60 socket is not assigned to the Abyssal Magistrate.");
            }
            else if (socket.SocketId == "map6-enemy-61-malachar")
            {
                Need(socket.Role == SpawnSocketRole.Boss &&
                    socket.SuggestedEnemyTypes.SequenceEqual(
                        new[] { EnemyType.Malachar }) &&
                    socket.IntroEligible &&
                    zone.Kind == DemonCityZoneKind.MainBossArena,
                    "Enemy 61 socket is not assigned to Malachar's throne arena.");
            }
            else
            {
                Need(socket.SuggestedEnemyTypes.Count == 0 &&
                    !socket.IntroEligible,
                    $"{socket.SocketId} must remain untyped until its enemy pass.");
            }

            Vector2 testSize = socket.Role == SpawnSocketRole.Boss
                ? new Vector2(160f, 180f)
                : new Vector2(56f, 80f);
            Vector2 safeSpawn = SideScrollingCollision.ResolveSafeEntrySpawn(
                socket.Position.X, testSize, map, 0f, 0f);
            Need(socket.PlacementBounds.Contains(safeSpawn.ToPoint()),
                $"{socket.SocketId} has no safe spawn inside its placement bounds.");

            if (socket.SocketId == "map6-enemy-61-malachar")
            {
                bossSockets++;
                Need(zone.Kind == DemonCityZoneKind.MainBossArena,
                    "Malachar's boss socket is outside the main arena.");
            }
            else
            {
                plannedEnemySockets++;
                Need(socket.SocketId is
                        "map6-enemy-51-placeholder" or
                        "map6-enemy-52-placeholder" or
                        "map6-enemy-53-placeholder" or
                        "map6-enemy-54-placeholder" or
                        "map6-enemy-55-placeholder" or
                        "map6-enemy-56-placeholder" or
                        "map6-enemy-57-placeholder" or
                        "map6-enemy-58-placeholder" or
                        "map6-enemy-59-placeholder" or
                        "map6-enemy-60-placeholder" ||
                    socket.Role == SpawnSocketRole.Unassigned,
                    $"{socket.SocketId} invents an enemy role before its design pass.");
            }
        }
        Need(plannedEnemySockets == 10 && bossSockets == 1,
            "Map 6 enemy and main-boss socket counts are invalid.");
        for (int enemyNumber = 51; enemyNumber <= 60; enemyNumber++)
        {
            Need(ids.Contains($"map6-enemy-{enemyNumber}-placeholder"),
                $"Map 6 is missing the reserved socket for enemy {enemyNumber}.");
        }
    }

    private static void ValidateImplementedEnemyPasses(DungeonMap map)
    {
        SpawnSocket enforcerSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-51-placeholder");
        SpawnSocket keeperSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-52-placeholder");
        SpawnSocket stalkerSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-53-placeholder");
        SpawnSocket collectorSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-54-placeholder");
        SpawnSocket servitorSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-55-placeholder");
        SpawnSocket cantorSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-56-placeholder");
        SpawnSocket wardenSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-57-placeholder");
        SpawnSocket harrierSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-58-placeholder");
        SpawnSocket butcherSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-59-placeholder");
        SpawnSocket magistrateSocket = map.SpawnSockets.First(socket =>
            socket.SocketId == "map6-enemy-60-placeholder");
        DungeonRoom enforcerRoom = map.FindRoomContaining(enforcerSocket.Position);
        DungeonRoom keeperRoom = map.FindRoomContaining(keeperSocket.Position);
        DungeonRoom stalkerRoom = map.FindRoomContaining(stalkerSocket.Position);
        DungeonRoom collectorRoom = map.FindRoomContaining(collectorSocket.Position);
        DungeonRoom servitorRoom = map.FindRoomContaining(servitorSocket.Position);
        DungeonRoom cantorRoom = map.FindRoomContaining(cantorSocket.Position);
        DungeonRoom wardenRoom = map.FindRoomContaining(wardenSocket.Position);
        DungeonRoom harrierRoom = map.FindRoomContaining(harrierSocket.Position);
        DungeonRoom butcherRoom = map.FindRoomContaining(butcherSocket.Position);
        DungeonRoom magistrateRoom = map.FindRoomContaining(magistrateSocket.Position);
        var enforcerContext = new EnemySpawnContext(
            enforcerSocket.Position, 7, 3, enforcerRoom.Id, false,
            GoblinVariant.Normal);
        var keeperContext = new EnemySpawnContext(
            keeperSocket.Position, 7, 3, keeperRoom.Id, false,
            GoblinVariant.Normal);
        var stalkerContext = new EnemySpawnContext(
            stalkerSocket.Position, 7, 3, stalkerRoom.Id, false,
            GoblinVariant.Normal);
        var collectorContext = new EnemySpawnContext(
            collectorSocket.Position, 7, 3, collectorRoom.Id, false,
            GoblinVariant.Normal);
        var servitorContext = new EnemySpawnContext(
            servitorSocket.Position, 7, 3, servitorRoom.Id, false,
            GoblinVariant.Normal);
        var cantorContext = new EnemySpawnContext(
            cantorSocket.Position, 7, 3, cantorRoom.Id, false,
            GoblinVariant.Normal);
        var wardenContext = new EnemySpawnContext(
            wardenSocket.Position, 7, 3, wardenRoom.Id, false,
            GoblinVariant.Normal);
        var harrierContext = new EnemySpawnContext(
            harrierSocket.Position, 7, 3, harrierRoom.Id, false,
            GoblinVariant.Normal);
        var butcherContext = new EnemySpawnContext(
            butcherSocket.Position, 7, 3, butcherRoom.Id, true,
            GoblinVariant.Normal);
        var magistrateContext = new EnemySpawnContext(
            magistrateSocket.Position, 7, 3, magistrateRoom.Id, true,
            GoblinVariant.Normal);
        Enemy enforcer = EnemyFactory.Create(
            EnemyType.DemonCityEnforcer, enforcerContext);
        Enemy keeper = EnemyFactory.Create(
            EnemyType.SoulLanternKeeper, keeperContext);
        Enemy stalker = EnemyFactory.Create(
            EnemyType.AshbladeStalker, stalkerContext);
        Enemy collector = EnemyFactory.Create(
            EnemyType.ChainCollector, collectorContext);
        Enemy servitor = EnemyFactory.Create(
            EnemyType.BloodworksServitor, servitorContext);
        Enemy cantor = EnemyFactory.Create(
            EnemyType.SigilCantor, cantorContext);
        Enemy warden = EnemyFactory.Create(
            EnemyType.VeilWarden, wardenContext);
        Enemy harrier = EnemyFactory.Create(
            EnemyType.GloomwingHarrier, harrierContext);
        Enemy butcher = EnemyFactory.Create(
            EnemyType.CathedralButcher, butcherContext);
        Enemy magistrate = EnemyFactory.Create(
            EnemyType.AbyssalMagistrate, magistrateContext);

        Need(enforcer is DemonCityEnemy && !enforcer.IsElite &&
            !enforcer.UsesProjectileAttack,
            "Enemy 51 factory or standard melee classification failed.");
        Need(keeper is DemonCityEnemy && !keeper.IsElite &&
            keeper.UsesProjectileAttack,
            "Enemy 52 factory or ranged classification failed.");
        Need(stalker is DemonCityEnemy && !stalker.IsElite &&
            !stalker.UsesProjectileAttack,
            "Enemy 53 factory or standard skirmisher classification failed.");
        Need(collector is DemonCityEnemy && !collector.IsElite &&
            !collector.UsesProjectileAttack,
            "Enemy 54 factory or standard controller classification failed.");
        Need(servitor is DemonCityIndustrialEnemy && !servitor.IsElite &&
            !servitor.UsesProjectileAttack &&
            servitor.MaxPoise > enforcer.MaxPoise,
            "Enemy 55 factory, heavy classification, or poise failed.");
        Need(cantor is DemonCityIndustrialEnemy && !cantor.IsElite &&
            cantor.UsesProjectileAttack && cantor.MaxPoise < servitor.MaxPoise,
            "Enemy 56 factory or ritual support classification failed.");
        Need(warden is DemonCityEliteEnemy && warden.IsElite &&
            !warden.IsFlying && warden.MaxPoise > enforcer.MaxPoise,
            "Enemy 57 factory, elite classification, or poise failed.");
        Need(harrier is DemonCityEliteEnemy && harrier.IsElite &&
            harrier.IsFlying && harrier.MaxPoise < warden.MaxPoise,
            "Enemy 58 factory, elite flight classification, or poise failed.");
        var harrierElite = (DemonCityEliteEnemy)harrier;
        EncounterZone harrierEncounter = map.EncounterZones.First(zone =>
            zone.ZoneId == "demon-city-gloomwing-harrier");
        harrierElite.SetAuthoredArenaBounds(harrierEncounter.ActivationBounds);
        Vector2 edgeRecovery =
            harrierElite.ResolveHarrierGroundTargetForValidation(12191f, map);
        Need(MathF.Abs(edgeRecovery.X - 12191f) > .01f &&
            MathF.Abs(edgeRecovery.X - 12191f) <=
                DemonCityEliteEnemy.GroundTargetSearchRadius &&
            harrierEncounter.ActivationBounds.Contains(
                edgeRecovery.ToPoint()) &&
            SideScrollingCollision.IsPositionFree(
                edgeRecovery, harrierElite.Size, map) &&
            SideScrollingCollision.IsSupported(
                edgeRecovery, harrierElite.Size, map),
            "Enemy 58 failed to replace an unsupported platform-edge X with " +
            "a nearby in-arena ground recovery target.");
        harrierElite.SnapToGround(harrierRoom);
        var flightPlayer = new DungeonAscendant.Player.Player(
            SideScrollingCollision.PlaceOnGround(
                harrierSocket.Position.X + 160f,
                new Vector2(
                    DungeonAscendant.Player.Player.BodyHurtboxWidth,
                    DungeonAscendant.Player.Player.BodyHurtboxHeight),
                harrierRoom))
        {
            DebugGodMode = true
        };
        var flightProjectiles = new ProjectileManager();
        var flightHazards = new RootHazardManager();
        bool sawTakeoff = false;
        bool sawDive = false;
        bool sawBrake = false;
        bool sawLanding = false;
        bool sawRecovery = false;
        for (int frame = 0; frame < 720; frame++)
        {
            harrierElite.Update(
                Frame, flightPlayer, map, flightProjectiles, flightHazards);
            harrierElite.ConstrainToAuthoredArena(
                harrierEncounter.ActivationBounds);
            sawTakeoff |= harrierElite.CombatState ==
                DemonCityEliteState.HarrierTakeoff;
            sawDive |= harrierElite.CombatState ==
                DemonCityEliteState.HarrierDiveCommit;
            sawBrake |= harrierElite.CombatState ==
                DemonCityEliteState.HarrierDiveBrake;
            sawLanding |= harrierElite.CombatState ==
                DemonCityEliteState.HarrierLand;
            sawRecovery |= harrierElite.CombatState ==
                DemonCityEliteState.HarrierGroundRecovery;
        }
        Need(sawTakeoff && sawDive && sawBrake && sawLanding && sawRecovery &&
            harrierEncounter.ActivationBounds.Contains(
                harrierElite.Position.ToPoint()),
            "Enemy 58 no longer completes its takeoff, dive, brake, landing, " +
            "and grounded recovery loop inside the authored arena.");
        Need(butcher is DemonCityBossEnemy && butcher.IsElite &&
            !butcher.CanDropLoot && butcher.MaxPoise > warden.MaxPoise,
            "Enemy 59 factory, sub-boss classification, or poise failed.");
        Need(magistrate is DemonCityBossEnemy && magistrate.IsElite &&
            magistrate.UsesProjectileAttack && !magistrate.CanDropLoot &&
            magistrate.MaxPoise > butcher.MaxPoise,
            "Enemy 60 factory, boss classification, or poise failed.");
        Need(AudioProfiles.GetEnemy(EnemyType.VeilWarden).Id ==
                "veil_warden" &&
            AudioProfiles.GetEnemy(EnemyType.GloomwingHarrier).Id ==
                "gloomwing_harrier" &&
            AudioProfiles.GetEnemy(EnemyType.VeilWarden)
                .GetClips(EnemySoundEvent.Special).Count > 0 &&
            AudioProfiles.GetEnemy(EnemyType.GloomwingHarrier)
                .GetClips(EnemySoundEvent.AttackWindup).Count > 0,
            "Enemies 57 and 58 are missing semantic audio profiles.");
        Need(AudioProfiles.GetEnemy(EnemyType.CathedralButcher).Id ==
                "cathedral_butcher" &&
            AudioProfiles.GetEnemy(EnemyType.CathedralButcher)
                .GetClips(EnemySoundEvent.AttackRelease).Count > 0 &&
            AudioProfiles.GetEnemy(EnemyType.AbyssalMagistrate).Id ==
                "abyssal_magistrate" &&
            AudioProfiles.GetEnemy(EnemyType.AbyssalMagistrate)
                .GetClips(EnemySoundEvent.Special).Count > 0,
            "Enemies 59 and 60 are missing semantic audio profiles.");
        Need(DemonCityEliteEnemySpriteRenderer.WardenVisualSize.X <
                DemonCityIndustrialEnemySpriteRenderer.ServitorVisualSize.X &&
            DemonCityEliteEnemySpriteRenderer.HarrierVisualSize.X >
                DemonCityEliteEnemySpriteRenderer.WardenVisualSize.X,
            "Enemies 57 and 58 lost their distinct elite silhouettes.");
        Need(DemonCityEnemy.CleaveWindupSeconds > 0f &&
            DemonCityEnemy.CleaveActiveSeconds > 0f &&
            DemonCityEnemy.CleaveRecoverySeconds > 0f &&
            DemonCityEnemy.BoltChargeSeconds > 0f &&
            DemonCityEnemy.BoltReleaseSeconds > 0f &&
            DemonCityEnemy.BoltRecoverySeconds > 0f &&
            DemonCityEnemy.TwinRipHitSeconds > 0f &&
            DemonCityEnemy.TwinRipRecoverySeconds > 0f &&
            DemonCityEnemy.HunterStepSeconds < 1f &&
            DemonCityEnemy.CrossingRecoverySeconds >
                DemonCityEnemy.CrossingActiveSeconds &&
            DemonCityEnemy.HookAimSeconds > 0f &&
            DemonCityEnemy.HookExtendSeconds > 0f &&
            DemonCityEnemy.HookRetractSeconds > 0f &&
            DemonCityEnemy.HookRecoverySeconds >
                DemonCityEnemy.HookResolveSeconds &&
            DemonCityEnemy.CollectorPullDistance <= 32f &&
            DemonCityEnemy.CollectorHookCooldownSeconds >= 5f,
            "Map 6 enemy attacks require tell, active, and recovery timing.");

        Need(DemonCityIndustrialEnemy.PressWindupSeconds >
                DemonCityIndustrialEnemy.PressActiveSeconds &&
            DemonCityIndustrialEnemy.PressRecoverySeconds >
                DemonCityIndustrialEnemy.PressActiveSeconds &&
            DemonCityIndustrialEnemy.VentCooldownSeconds >= 5f &&
            DemonCityIndustrialEnemy.BindingTelegraphSeconds >
                DemonCityIndustrialEnemy.BindingCastSeconds &&
            DemonCityIndustrialEnemy.BindingActiveSeconds < 2f &&
            DemonCityIndustrialEnemy.WarCooldownSeconds >
                DemonCityIndustrialEnemy.WarBuffSeconds &&
            DemonCityIndustrialEnemy.LanceAimSeconds > 0f &&
            DemonCityIndustrialEnemy.LanceRecoverySeconds > 0f &&
            RootHazardManager.MaximumBindingSigilsPerOwner == 1,
            "Enemies 55 and 56 require bounded tells, effects, and recoveries.");

        Need(DemonCityEliteEnemy.CutWindupSeconds >
                DemonCityEliteEnemy.CutActiveSeconds &&
            DemonCityEliteEnemy.CutRecoverySeconds >
                DemonCityEliteEnemy.CutActiveSeconds &&
            DemonCityEliteEnemy.ThrustWindupSeconds >
                DemonCityEliteEnemy.ThrustActiveSeconds &&
            DemonCityEliteEnemy.GuardCooldownSeconds >
                DemonCityEliteEnemy.GuardActiveSeconds &&
            DemonCityEliteEnemy.ReprisalPauseSeconds >
                DemonCityEliteEnemy.ReprisalActiveSeconds &&
            DemonCityEliteEnemy.DiveSetupSeconds >
                DemonCityEliteEnemy.DiveCommitSeconds &&
            DemonCityEliteEnemy.DiveBrakeSeconds > 0f &&
            DemonCityEliteEnemy.GroundRecoverySeconds >
                DemonCityEliteEnemy.RendStrikeSeconds &&
            DemonCityEliteEnemy.MaximumFlightHeight <= 180f &&
            DemonCityEliteEnemy.MaximumFlightOffset <= 340f,
            "Enemies 57 and 58 require bounded tells, defense, flight, and recovery.");

        Need(map.EncounterZones.Any(zone =>
                zone.SuggestedEnemyTypes.Contains(EnemyType.AshbladeStalker)) &&
            map.EncounterZones.Any(zone =>
                zone.SuggestedEnemyTypes.Contains(EnemyType.ChainCollector)) &&
            map.EncounterZones.Any(zone =>
                zone.SuggestedEnemyTypes.Contains(EnemyType.BloodworksServitor) &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.SigilCantor)) &&
            map.EncounterZones.Any(zone =>
                zone.IsEliteZone &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.VeilWarden)) &&
            map.EncounterZones.Any(zone =>
                zone.IsEliteZone &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.GloomwingHarrier)) &&
            map.EncounterZones.Any(zone =>
                zone.IsEliteZone && zone.ZoneId ==
                    "demon-city-cathedral-butcher" &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.CathedralButcher)) &&
            map.EncounterZones.Any(zone =>
                zone.IsBossZone && zone.ZoneId ==
                    "demon-city-abyssal-magistrate" &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.AbyssalMagistrate)) &&
            map.EncounterZones.Any(zone =>
                zone.IsBossZone && zone.ZoneId == "demon-city-malachar" &&
                zone.SuggestedEnemyTypes.Contains(EnemyType.Malachar)),
            "Implemented Map 6 enemies are missing from their city encounters.");
    }

    private static void ValidateFinalResources()
    {
        (EnemyType Type, string Key, Vector2 VisualSize)[] roster =
        {
            (EnemyType.DemonCityEnforcer, "demon_city_enforcer",
                DemonCityEnemySpriteRenderer.EnforcerVisualSize),
            (EnemyType.SoulLanternKeeper, "soul_lantern_keeper",
                DemonCityEnemySpriteRenderer.KeeperVisualSize),
            (EnemyType.AshbladeStalker, "ashblade_stalker",
                DemonCityEnemySpriteRenderer.StalkerVisualSize),
            (EnemyType.ChainCollector, "chain_collector",
                DemonCityEnemySpriteRenderer.CollectorVisualSize),
            (EnemyType.BloodworksServitor, "bloodworks_servitor",
                DemonCityIndustrialEnemySpriteRenderer.ServitorVisualSize),
            (EnemyType.SigilCantor, "sigil_cantor",
                DemonCityIndustrialEnemySpriteRenderer.CantorVisualSize),
            (EnemyType.VeilWarden, "veil_warden",
                DemonCityEliteEnemySpriteRenderer.WardenVisualSize),
            (EnemyType.GloomwingHarrier, "gloomwing_harrier",
                DemonCityEliteEnemySpriteRenderer.HarrierVisualSize),
            (EnemyType.CathedralButcher, "cathedral_butcher",
                DemonCityBossEnemySpriteRenderer.ButcherVisualSize),
            (EnemyType.AbyssalMagistrate, "abyssal_magistrate",
                DemonCityBossEnemySpriteRenderer.MagistrateVisualSize),
            (EnemyType.Malachar, "malachar",
                MalacharEnemySpriteRenderer.VisualSize)
        };
        IReadOnlyDictionary<string, string> english =
            Loc.GetStrings(Language.English);
        IReadOnlyDictionary<string, string> vietnamese =
            Loc.GetStrings(Language.Vietnamese);

        foreach ((EnemyType type, string key, Vector2 visualSize) in roster)
        {
            string nameKey = $"enemy.{key}.name";
            string titleKey = $"enemy.{key}.title";
            Need(english.TryGetValue(nameKey, out string englishName) &&
                !string.IsNullOrWhiteSpace(englishName) &&
                english.TryGetValue(titleKey, out string englishTitle) &&
                !string.IsNullOrWhiteSpace(englishTitle) &&
                vietnamese.TryGetValue(nameKey, out string vietnameseName) &&
                !string.IsNullOrWhiteSpace(vietnameseName) &&
                vietnamese.TryGetValue(titleKey, out string vietnameseTitle) &&
                !string.IsNullOrWhiteSpace(vietnameseTitle),
                $"Map 6 {type} is missing EN/VI name or title localization.");
            EnemySoundProfile audio = AudioProfiles.GetEnemy(type);
            Need(!string.IsNullOrWhiteSpace(audio.Id) &&
                Enum.GetValues<EnemySoundEvent>().Any(soundEvent =>
                    audio.GetClips(soundEvent).Count > 0),
                $"Map 6 {type} has no resolvable audio profile.");
            Need(visualSize.X > 0f && visualSize.Y > 0f,
                $"Map 6 {type} has no resolvable renderer footprint.");
        }
    }

    private static void ValidateMalachar(DungeonMap map)
    {
        SpawnSocket socket = map.SpawnSockets.First(candidate =>
            candidate.SocketId == "map6-enemy-61-malachar");
        DungeonRoom room = map.FindRoomContaining(socket.Position);
        var context = new EnemySpawnContext(
            socket.Position, 9, 3, room.Id, true, GoblinVariant.Normal);
        Enemy created = EnemyFactory.Create(EnemyType.Malachar, context);
        Need(created is MalacharEnemy && created.IsElite &&
            created.UsesProjectileAttack && !created.CanDropLoot &&
            created.MaxPoise > 220f,
            "Enemy 61 factory, sovereign classification, or poise failed.");
        var malachar = (MalacharEnemy)created;
        EncounterZone encounter = map.EncounterZones.First(zone =>
            zone.ZoneId == "demon-city-malachar");
        Need(encounter.IsBossZone && encounter.MaxConcurrentEnemies == 1 &&
            encounter.SuggestedEnemyTypes.SequenceEqual(
                new[] { EnemyType.Malachar }),
            "Malachar does not own one dedicated authored boss encounter.");
        malachar.SetAuthoredArenaBounds(encounter.ActivationBounds);
        malachar.SnapToGround(room);

        var player = new DungeonAscendant.Player.Player(
            SideScrollingCollision.ResolveSafeEntrySpawn(
                socket.Position.X + 150f,
                new Vector2(
                    DungeonAscendant.Player.Player.BodyHurtboxWidth,
                    DungeonAscendant.Player.Player.BodyHurtboxHeight),
                map, 0f))
        {
            DebugGodMode = true
        };
        var projectiles = new ProjectileManager();
        var hazards = new RootHazardManager();
        var seen = new HashSet<MalacharAttackKind>();
        for (int frame = 0; frame < 1800; frame++)
        {
            if (frame % 360 == 0)
            {
                float offset = frame % 720 == 0 ? 330f : 140f;
                player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
                    malachar.Position.X + offset,
                    player.Size, map, 0f));
            }
            malachar.Update(Frame, player, map, projectiles, hazards);
            seen.Add(malachar.AttackKind);
        }
        Need(malachar.BossIntroCompleted &&
            seen.Contains(MalacharAttackKind.SovereignCut) &&
            seen.Contains(MalacharAttackKind.KingsAdvance) &&
            seen.Contains(MalacharAttackKind.RisingDivide) &&
            malachar.ComboHitCount <= MalacharEnemy.MaximumCutHits &&
            malachar.LastAbyssStepDistance <=
                MalacharEnemy.MaximumAbyssStepDistance &&
            !malachar.AbyssStepDealsArrivalDamage,
            "Malachar Phase 1 lost a finite tell, combo, or bounded step.");

        while (malachar.CombatState != MalacharState.Ready)
            malachar.Update(Frame, player, map, projectiles, hazards);
        malachar.ReceiveDamage(
            malachar.CurrentHealth - malachar.MaxHealth * 69 / 100);
        for (int frame = 0; frame < 1500; frame++)
        {
            malachar.Update(Frame, player, map, projectiles, hazards);
            seen.Add(malachar.AttackKind);
        }
        Need(malachar.CombatPhase == 2 &&
            malachar.PhaseTransitionCount == 1 &&
            seen.Contains(MalacharAttackKind.DominionArc) &&
            seen.Contains(MalacharAttackKind.AbyssalPillar) &&
            !malachar.DominionArcIsHoming,
            "Malachar Phase 2 lost its one-shot transition or bounded control kit.");

        while (malachar.CombatState != MalacharState.Ready)
            malachar.Update(Frame, player, map, projectiles, hazards);
        malachar.ReceiveDamage(
            malachar.CurrentHealth - malachar.MaxHealth * 34 / 100);
        for (int frame = 0; frame < 1900; frame++)
        {
            malachar.Update(Frame, player, map, projectiles, hazards);
            seen.Add(malachar.AttackKind);
        }
        Need(malachar.CombatPhase == 3 &&
            malachar.PhaseTransitionCount == 2 &&
            seen.Contains(MalacharAttackKind.SovereignBreak) &&
            seen.Contains(MalacharAttackKind.CrownlessRush) &&
            seen.Contains(MalacharAttackKind.FinalDominion) &&
            seen.Contains(MalacharAttackKind.MalacharsEdict) &&
            malachar.FinalDominionHasSafeLanes &&
            RootHazardManager.MaximumMalacharHazardsPerOwner == 7,
            "Malachar Phase 3 lost a signature, safe lane, or bounded hazard rule.");
        Need(AudioProfiles.GetEnemy(EnemyType.Malachar).Id == "malachar" &&
            AudioProfiles.GetEnemy(EnemyType.Malachar)
                .GetClips(EnemySoundEvent.Special).Count > 0 &&
            MalacharEnemySpriteRenderer.VisualSize.Y >
                DemonCityBossEnemySpriteRenderer.MagistrateVisualSize.Y,
            "Malachar is missing main-boss audio or visual hierarchy.");

        int defeatEvents = 0;
        malachar.SovereignDefeated += _ => defeatEvents++;
        while (malachar.CombatState != MalacharState.Ready)
            malachar.Update(Frame, player, map, projectiles, hazards);
        malachar.ReceiveDamage(int.MaxValue);
        malachar.ReceiveDamage(int.MaxValue);
        Need(!malachar.IsAlive && defeatEvents == 1 &&
            malachar.BossCompletionCount == 1 && hazards.Hazards.Count == 0 &&
            projectiles.Projectiles.Count == 0,
            "Malachar death or owned-effect cleanup did not resolve exactly once.");

        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 6610);
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 6, 0);
        EncounterZone liveEncounter = session.CurrentDungeon.EncounterZones
            .First(zone => zone.ZoneId == "demon-city-malachar");
        Enemy liveBoss = TriggerEncounter(
            session, liveEncounter, EnemyType.Malachar);
        session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
            liveBoss.Position.X + 150f,
            session.Player.Size,
            session.CurrentDungeon,
            0f));
        for (int frame = 0; frame < 300; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
        Need(liveBoss.CanBeTargeted,
            "Malachar's short introduction did not hand control to combat.");
        liveBoss.ReceiveDamage(int.MaxValue);
        for (int frame = 0; frame < 180; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Map06Cleared && session.BossDefeated &&
            session.IsExitUnlocked &&
            session.WildForestEncounters.IsCleared("demon-city-malachar"),
            "Malachar defeat did not persist the exact-once Map 6 clear state.");
        session.Player.MoveTo(session.ExitPosition);
        session.Update(Frame, new KeyboardState(), new MouseState());
        session.Update(Frame, new KeyboardState(Keys.E), new MouseState());
        Need(session.Map06ExitPrepared && session.DungeonDepth == 6 &&
            session.CurrentDungeon.IsDemonCity,
            "The prepared Map 7 endpoint did not unlock without constructing Map 7.");
    }

    private static void ValidateIndustrialRitualSystems(DungeonMap map)
    {
        DemonCityZoneDefinition district = map.DemonCityZones[4];
        var hazards = new RootHazardManager();
        object owner = new();
        Vector2 sigilPosition = new(
            district.Bounds.Left + 1020f,
            district.GroundY - 6f);
        int roomId = map.FindRoomContaining(sigilPosition).Id;
        Need(hazards.TryAddBindingSigil(
                sigilPosition,
                new Vector2(142f, 20f),
                DemonCityIndustrialEnemy.BindingTelegraphSeconds,
                DemonCityIndustrialEnemy.BindingActiveSeconds,
                .72f,
                .28f,
                roomId,
                owner,
                map) &&
            !hazards.TryAddBindingSigil(
                sigilPosition + new Vector2(40f, 0f),
                new Vector2(142f, 20f),
                .2f,
                .4f,
                .72f,
                .28f,
                roomId,
                owner,
                map),
            "Sigil Cantor created more than one primary binding sigil.");

        Vector2 playerPosition = SideScrollingCollision.ResolveSafeEntrySpawn(
            sigilPosition.X,
            new Vector2(
                DungeonAscendant.Player.Player.BodyHurtboxWidth,
                DungeonAscendant.Player.Player.BodyHurtboxHeight),
            map,
            0f);
        var player = new DungeonAscendant.Player.Player(playerPosition);
        hazards.Update(new GameTime(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(
                DemonCityIndustrialEnemy.BindingTelegraphSeconds + .02f)),
            player);
        Need(hazards.Hazards.Count == 1 &&
            hazards.Hazards[0].State == RootHazardState.Active,
            "Binding Sigil did not progress from marker to active zone.");
        hazards.Update(new GameTime(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(
                DemonCityIndustrialEnemy.BindingActiveSeconds + .02f)),
            player);
        Need(hazards.Hazards.Count == 0,
            "Binding Sigil did not expire after its bounded active window.");

        Need(hazards.TryAddBindingSigil(
                sigilPosition,
                new Vector2(142f, 20f),
                .5f,
                .5f,
                .72f,
                .28f,
                roomId,
                owner,
                map),
            "Binding Sigil interruption fixture could not create its marker.");
        hazards.RemoveOwnedBy(owner);
        Need(hazards.Hazards.Count == 0,
            "Interrupted or dead Cantor left an owned sigil behind.");

        var projectiles = new ProjectileManager();
        projectiles.SpawnSigilLance(
            sigilPosition + new Vector2(0f, -70f),
            Vector2.UnitX,
            damage: 10,
            roomId);
        Need(projectiles.Projectiles.Count == 1 &&
            projectiles.Projectiles[0].Type == ProjectileType.SigilLance &&
            !projectiles.Projectiles[0].PenetratesTerrain,
            "Sigil Lance is not a visible terrain-bound projectile.");
    }

    private static void ValidateBossArenaAndExit(DungeonMap map)
    {
        DemonCityZoneDefinition arena = map.DemonCityZones[8];
        DungeonRoom arenaRoom = map.FindRoomContaining(
            arena.Bounds.Center.ToVector2());
        Need(arena.Kind == DemonCityZoneKind.MainBossArena &&
            arena.Bounds.Width >= 2000 && arena.Bounds.Height >= 600,
            "Map 6 main boss arena lacks combat or camera clearance.");
        foreach (Platform platform in arenaRoom.Platforms)
        {
            Need(platform.Kind != PlatformKind.Raised,
                "Map 6 main boss arena must remain free of raised collision traps.");
        }

        Need(map.AuthoredExitPosition.HasValue &&
            map.ExitRoom.Bounds.Contains(map.AuthoredExitPosition.Value.ToPoint()),
            "Map 6 lacks its intentional Map 7 endpoint.");
        bool hasSealedGate = false;
        foreach (DemonCityPropDefinition prop in map.DemonCityProps)
        {
            hasSealedGate |= prop.Kind == DemonCityPropKind.SealedGate &&
                prop.ZoneId == map.DemonCityZones[9].ZoneId;
        }
        Need(hasSealedGate,
            "Map 6 endpoint lacks a visible sealed route toward Map 7.");
    }

    private static void ValidateSessionAndMapFiveTransition()
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 6510);
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 6, 0);
        Need(session.DungeonDepth == 6 && session.CurrentDungeon.IsDemonCity &&
            session.CurrentRegion == RegionType.DemonCity &&
            session.CurrentMapName.Length > 0 && !session.ShouldRenderBoss &&
            session.Enemies.Enemies.Count == 0,
            "Map 6 GameSession/developer-load registration failed.");

        for (int zone = 0; zone < session.CurrentDungeon.DemonCityZones.Count; zone++)
        {
            session.ExecuteDeveloperCommand(DeveloperCommand.TeleportZone, 6, zone);
            Need(session.GetCurrentZoneIndex() == zone &&
                SideScrollingCollision.IsPositionFree(
                    session.Player.Position, session.Player.Size,
                    session.CurrentDungeon) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Player.Bounds) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
                $"Map 6 zone {zone} has an unsafe player/camera arrival.");
        }

        ValidateLiveBossTraversal(session);

        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 5, 0);
        Need(!session.Map05ExecutionerDefeated && !session.BossDefeated &&
            session.IsExitUnlocked,
            "Map 5 transition still depends on final encounter completion.");
        for (int frame = 0; frame < 60; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
        session.Player.MoveTo(session.ExitPosition);
        session.Update(Frame, new KeyboardState(Keys.E), new MouseState());
        Need(session.DungeonDepth == 6 && session.CurrentDungeon.IsDemonCity,
            "Map 5 open exit did not resolve to the Map 6 generator.");
    }

    private static void ValidateEncounterTraversalIndependence(DungeonMap map)
    {
        var director = new WildForestEncounterDirector();
        var enemies = new EnemyManager(6505);
        director.Reset(map);
        foreach (EncounterZone encounter in map.EncounterZones)
        {
            Vector2 playerPosition = SideScrollingCollision.ResolveSafeEntrySpawn(
                encounter.ActivationBounds.Left + 4f,
                new Vector2(
                    DungeonAscendant.Player.Player.BodyHurtboxWidth,
                    DungeonAscendant.Player.Player.BodyHurtboxHeight),
                map,
                0f);
            for (int frame = 0; frame < 12; frame++)
            {
                director.Update(
                    new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05f)),
                    playerPosition,
                    map,
                    enemies,
                    enemyLevel: 8,
                    worldTier: 3);
            }
            Need(director.IsTriggered(encounter.ZoneId),
                $"Map 6 encounter {encounter.ZoneId} was gated by an earlier live encounter.");
        }
        EnemyType[] requiredAlive =
        {
            EnemyType.DemonCityEnforcer,
            EnemyType.SoulLanternKeeper,
            EnemyType.AshbladeStalker,
            EnemyType.ChainCollector,
            EnemyType.BloodworksServitor,
            EnemyType.SigilCantor,
            EnemyType.VeilWarden,
            EnemyType.GloomwingHarrier,
            EnemyType.CathedralButcher,
            EnemyType.AbyssalMagistrate,
            EnemyType.Malachar
        };
        foreach (EnemyType type in requiredAlive)
        {
            Enemy[] matches = enemies.Enemies.Where(enemy =>
                enemy.IsAlive && enemy.Type == type).ToArray();
            Need(matches.Length == 1,
                $"Map 6 traversal expected exactly one live {type} spawn.");

            EncounterZone encounter = map.EncounterZones.Single(zone =>
                zone.SuggestedEnemyTypes.Contains(type));
            Need(encounter.ActivationBounds.Contains(
                    matches[0].Position.ToPoint()),
                $"Map 6 {type} escaped its authored encounter bounds.");

            if (type is EnemyType.GloomwingHarrier or EnemyType.Malachar)
                continue;

            float outsideX = encounter.ActivationBounds.Left - 180f;
            if (outsideX <= map.WorldBounds.Left)
                outsideX = encounter.ActivationBounds.Right + 180f;
            var outsidePlayer = new DungeonAscendant.Player.Player(
                new Vector2(outsideX, matches[0].Position.Y))
            {
                DebugGodMode = true
            };
            Vector2 playerBefore = outsidePlayer.Position;
            matches[0].Update(
                Frame,
                outsidePlayer,
                map,
                new ProjectileManager(),
                new RootHazardManager());
            Need(matches[0].IsReturningToAuthoredHome &&
                outsidePlayer.Position == playerBefore,
                $"Map 6 {type} leash did not disengage without moving the player.");
        }
    }

    private static void ValidateLiveBossTraversal(GameSession session)
    {
        session.Player.DebugGodMode = true;
        EncounterZone butcherEncounter = session.CurrentDungeon.EncounterZones
            .First(zone => zone.ZoneId == "demon-city-cathedral-butcher");
        EncounterZone magistrateEncounter = session.CurrentDungeon.EncounterZones
            .First(zone => zone.ZoneId == "demon-city-abyssal-magistrate");
        EncounterZone malacharEncounter = session.CurrentDungeon.EncounterZones
            .First(zone => zone.ZoneId == "demon-city-malachar");
        Enemy butcher = TriggerEncounter(
            session, butcherEncounter, EnemyType.CathedralButcher);
        Enemy magistrate = TriggerEncounter(
            session, magistrateEncounter, EnemyType.AbyssalMagistrate);
        Enemy malachar = TriggerEncounter(
            session, malacharEncounter, EnemyType.Malachar);

        Enemy[] bosses = { butcher, magistrate, malachar };
        foreach (Enemy boss in bosses)
        {
            session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
                boss.Position.X,
                session.Player.Size,
                session.CurrentDungeon,
                0f));
            int introFrames = boss is MalacharEnemy ? 300 : 100;
            for (int frame = 0; frame < introFrames; frame++)
                session.Update(Frame, new KeyboardState(), new MouseState());
        }
        bool butcherIntro = ((DemonCityBossEnemy)butcher).BossIntroCompleted;
        bool magistrateIntro =
            ((DemonCityBossEnemy)magistrate).BossIntroCompleted;
        bool malacharIntro = ((MalacharEnemy)malachar).BossIntroCompleted;
        Need(butcherIntro && magistrateIntro && malacharIntro,
            "Map 6 boss introductions did not complete exactly once " +
            $"(59={butcherIntro}, 60={magistrateIntro}, 61={malacharIntro}).");
        foreach (Enemy boss in bosses)
            boss.UpdateHealthBarPresentation(Frame, boss.Position, true);
        Need(ReferenceEquals(GameRenderer.SelectDemonCityBossBarTarget(
                    bosses, butcher.Position), butcher) &&
            ReferenceEquals(GameRenderer.SelectDemonCityBossBarTarget(
                    bosses, magistrate.Position), magistrate) &&
            ReferenceEquals(GameRenderer.SelectDemonCityBossBarTarget(
                    bosses, malachar.Position), malachar),
            "Map 6 boss UI did not select one nearest/current live boss.");

        DemonCityZoneDefinition approach = session.CurrentDungeon.DemonCityZones[8];
        Vector2 beyondBosses = SideScrollingCollision.ResolveSafeEntrySpawn(
            approach.Bounds.Left + 180f,
            session.Player.Size,
            session.CurrentDungeon,
            0f);
        session.Player.MoveTo(beyondBosses);
        float expectedX = session.Player.Position.X;
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(MathF.Abs(session.Player.Position.X - expectedX) < .5f &&
            butcher.IsAlive && magistrate.IsAlive && malachar.IsAlive,
            "Map 6 moved the player back into a live boss encounter.");
        Need(butcherEncounter.ActivationBounds.Contains(butcher.Position.ToPoint()) &&
            magistrateEncounter.ActivationBounds.Contains(
                magistrate.Position.ToPoint()) &&
            malacharEncounter.ActivationBounds.Contains(
                malachar.Position.ToPoint()),
            "Map 6 boss enemy bounds were lost while player containment was removed.");
    }

    private static Enemy TriggerEncounter(
        GameSession session,
        EncounterZone encounter,
        EnemyType expectedType)
    {
        session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
            encounter.ActivationBounds.Left + 4f,
            session.Player.Size,
            session.CurrentDungeon,
            0f));
        for (int frame = 0; frame < 36; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
        Enemy target = session.Enemies.Enemies.FirstOrDefault(enemy =>
            enemy.IsAlive && enemy.Type == expectedType);
        Need(target != null,
            $"Map 6 traversal fixture did not start {encounter.ZoneId}.");
        return target;
    }

    private static void ValidateCheckpointLoad(DungeonMap map)
    {
        string directory = Path.Combine(Path.GetTempPath(),
            "DungeonAscendant-map6-foundation-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");
        DemonCityCheckpointDefinition checkpoint = map.DemonCityCheckpoints[2];
        try
        {
            var manager = new SaveSlotManager(path);
            Need(manager.Store(new SaveSlotData
            {
                SlotIndex = 0,
                Occupied = true,
                CharacterName = "MAP6 TEST",
                Appearance =
                    DungeonAscendant.Player.CharacterAppearance.CreateDefault(),
                Level = 7,
                DungeonDepth = 6,
                CurrentMapId = "map-06",
                CurrentArea = "MA THANH / DEMON CITY",
                CheckpointX = checkpoint.Position.X,
                CheckpointY = checkpoint.Position.Y,
                HasCheckpoint = true,
                Map01Cleared = true,
                Map02Cleared = true,
                Map03Cleared = true,
                Map04Cleared = true,
                Map05BallistaDefeated = true,
                Map05ExecutionerDefeated = true,
                Map06ButcherDefeated = true,
                Map06MagistrateDefeated = true,
                Map06Cleared = true,
                Map06ExitPrepared = true,
                BossDefeated = true,
                LastPlayedUtc = DateTimeOffset.UtcNow
            }), "Map 6 checkpoint fixture could not create a save.");

            var session = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 6606,
                savePath: path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.DungeonDepth == 6 && session.CurrentDungeon.IsDemonCity &&
                Vector2.DistanceSquared(session.Player.Position,
                    checkpoint.Position) < 1f,
                "Map 6 checkpoint did not restore through the existing save system.");
            Need(session.Map06ButcherDefeated &&
                session.Map06MagistrateDefeated && session.Map06Cleared &&
                session.Map06ExitPrepared && session.IsExitUnlocked &&
                session.WildForestEncounters.IsCleared(
                    "demon-city-cathedral-butcher") &&
                session.WildForestEncounters.IsCleared(
                    "demon-city-abyssal-magistrate") &&
                session.WildForestEncounters.IsCleared("demon-city-malachar"),
                "Map 6 boss, clear, or prepared-exit persistence did not restore.");

            EnemyType[] defeatedBosses =
            {
                EnemyType.CathedralButcher,
                EnemyType.AbyssalMagistrate,
                EnemyType.Malachar
            };
            foreach (EnemyType defeatedBoss in defeatedBosses)
            {
                EncounterZone encounter = session.CurrentDungeon.EncounterZones
                    .Single(zone =>
                        zone.SuggestedEnemyTypes.Contains(defeatedBoss));
                session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
                    encounter.ActivationBounds.Left + 4f,
                    session.Player.Size,
                    session.CurrentDungeon,
                    0f));
                for (int frame = 0; frame < 12; frame++)
                    session.Update(Frame, new KeyboardState(), new MouseState());
                Need(!session.Enemies.Enemies.Any(enemy =>
                        enemy.IsAlive && enemy.Type == defeatedBoss),
                    $"Persisted Map 6 boss {defeatedBoss} respawned.");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static DemonCityZoneDefinition FindZone(DungeonMap map, string id)
    {
        foreach (DemonCityZoneDefinition zone in map.DemonCityZones)
            if (zone.ZoneId == id)
                return zone;
        return null;
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame, new KeyboardState(key), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
    }

    private static int MandatoryGroundTop(DungeonMap map, int x)
    {
        int result = int.MaxValue;
        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Ground ||
                x < platform.Bounds.Left || x >= platform.Bounds.Right)
            {
                continue;
            }
            result = Math.Min(result, platform.Bounds.Top);
        }
        return result;
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
