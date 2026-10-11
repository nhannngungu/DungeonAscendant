using System;
using System.Collections.Generic;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Dungeon;

public static class FallenCityMapValidation
{
    private static readonly GameTime Frame = new(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new FallenCityMapGenerator().Generate();
        Need(map.IsFallenCity && map.FallenCityZones.Count == 10, "Map 3 needs ten Fallen City zones.");
        Need(map.WorldBounds.Width == FallenCityMapGenerator.WorldWidth && map.StartRoom != null && map.BossRoom != null && map.ExitRoom == map.BossRoom, "Map 3 world/start/boss/exit contract failed.");
        Need(map.EncounterZones.Count == 10 && map.SpawnProfiles.Count == 10 && map.SpawnSockets.Count >= 24, "Map 3 encounter ecology is incomplete.");
        Need(map.CityAlarmObjects.Count >= 3 && map.CityReinforcements.Count >= 3, "City Alarm objects or reinforcements are incomplete.");
        Need(map.CollapsingStructures.Count >= 5 && map.RouteFeatures.Count >= 8, "Collapsing city or side-route metadata is incomplete.");
        foreach (CollapsingStructureDefinition structure in map.CollapsingStructures)
        {
            Need(!structure.IsMandatorySupport, $"{structure.StructureId} incorrectly supports the mandatory route.");
            if (structure.HasCollision)
            {
                Need(!SideScrollingCollision.IsPositionFree(
                        structure.Bounds.Center.ToVector2(),
                        new Vector2(4f, 4f),
                        map),
                    $"{structure.StructureId} intact collision is missing.");
            }
        }
        ValidateContinuousGround(map);
        ValidateActualPlayerTraversal(map);
        ValidateRosterAndSpawns(map);
        FallenCityEnemyPolishValidation.ValidateOrThrow(map);
        FallenCityEnemyPolishPass2Validation.ValidateOrThrow(map);
        FallenCityEnemyPolishPass3Validation.ValidateOrThrow(map);
        FallenCityEnemyPolishPass4Validation.ValidateOrThrow(map);
        FallenCityEnemyPolishPass5Validation.ValidateOrThrow(map);
        ValidateAlarmAndCollapse(map);
        ValidateStatusAndDeathCleanup(map);
        ValidateBossPhases();
        ValidateMapTwoTransition();
        ValidateDeathRestartCleanup();
    }

    private static void ValidateContinuousGround(DungeonMap map)
    {
        int previous = int.MaxValue;
        for (int x = map.WorldBounds.Left + 4; x < map.WorldBounds.Right - 4; x += 8)
        {
            int top = MandatoryTop(map, x);
            Need(top != int.MaxValue, $"Fallen City main route gap near X={x}.");
            if (previous != int.MaxValue) Need(Math.Abs(top - previous) <= 32, $"Fallen City main route step is unsafe near X={x}.");
            previous = top;
        }
    }

    private static void ValidateActualPlayerTraversal(DungeonMap map)
    {
        Vector2 size = new(Player.Player.BodyHurtboxWidth, Player.Player.BodyHurtboxHeight);
        Vector2 start = SideScrollingCollision.ResolveSafeEntrySpawn(map.WorldBounds.Left + 105f, size, map, 0f);
        var player = new Player.Player(start);
        player.UpdateSideScrollingMovement(Frame, new KeyboardState(), false, map);
        float target = map.ExitRoom.Bounds.Right - 160f;
        float priorX = player.Position.X;
        int stillFrames = 0;
        for (int frame = 0; frame < 7200 && player.Position.X < target; frame++)
        {
            bool jump = stillFrames >= 8 && player.IsGrounded;
            player.UpdateSideScrollingMovement(Frame, new KeyboardState(Keys.D), jump, map);
            if (player.Position.X <= priorX + .05f) stillFrames++; else stillFrames = 0;
            priorX = player.Position.X;
        }
        Need(player.Position.X >= target, "Actual Player A/D + Space simulation could not traverse Map 3 without dash or attacks.");
        Need(map.WorldBounds.Contains(player.Bounds), "Actual Player traversal left the Map 3 world bounds.");
        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Raised) continue;
            Need(platform.Bounds.Width >= Player.Player.BodyHurtboxWidth * 2f, "A Fallen City side-path landing is narrower than the Player collider.");
            Need(HasSafeRaisedLanding(platform, size, map),
                "A Fallen City side-path landing has blocked standing clearance.");
        }
    }

    private static bool HasSafeRaisedLanding(
        Platform platform,
        Vector2 playerSize,
        DungeonMap map)
    {
        float x = platform.Bounds.Center.X;
        Vector2 platformLanding = new(
            x,
            platform.Bounds.Top - playerSize.Y / 2f);
        if (SideScrollingCollision.IsPositionFree(
            platformLanding, playerSize, map))
        {
            return true;
        }

        foreach (CollapsingStructureDefinition structure in
            map.CollapsingStructures)
        {
            if (!map.IsCollapsingStructureCollisionActive(structure) ||
                x - playerSize.X / 2f < structure.Bounds.Left ||
                x + playerSize.X / 2f > structure.Bounds.Right ||
                Math.Abs(structure.Bounds.Bottom - platform.Bounds.Top) > 8)
            {
                continue;
            }
            Vector2 structureLanding = new(
                x,
                structure.Bounds.Top - playerSize.Y / 2f);
            return SideScrollingCollision.IsPositionFree(
                    structureLanding, playerSize, map) &&
                SideScrollingCollision.IsSupported(
                    structureLanding, playerSize, map);
        }
        return false;
    }

    private static void ValidateRosterAndSpawns(DungeonMap map)
    {
        var roster = new HashSet<EnemyType>();
        foreach (EncounterZone zone in map.EncounterZones) foreach (EnemyType type in zone.SuggestedEnemyTypes) roster.Add(type);
        EnemyType[] required = { EnemyType.FallenMilitiaman, EnemyType.RuinedCrossbowman, EnemyType.RabidHound, EnemyType.PlagueCarrier, EnemyType.BellWatcher, EnemyType.StreetExecutioner, EnemyType.RoyalGuard, EnemyType.AshKnight, EnemyType.WarBishop, EnemyType.BrokenKing };
        foreach (EnemyType type in required) Need(roster.Contains(type), $"Map 3 omits {type}.");
        foreach (EncounterZone zone in map.EncounterZones)
        {
            var manager = new EnemyManager(3303);
            manager.Reset(map, 4, 1, RegionType.RuinedCity, suppressProceduralSpawns: true);
            var director = new WildForestEncounterDirector();
            director.Reset(map);
            Vector2 trigger = new(zone.ActivationBounds.Left + 3, zone.ActivationBounds.Center.Y);
            for (int frame = 0; frame < 40; frame++) director.Update(Frame, trigger, map, manager, 4, 1);
            Need(director.IsTriggered(zone.ZoneId), $"{zone.ZoneId} has no usable authored spawn socket.");
            Need(manager.Enemies.Count > 0 && manager.Enemies.Count <= zone.MaxConcurrentEnemies, $"{zone.ZoneId} violates its concurrent enemy cap.");
            foreach (Enemy enemy in manager.Enemies)
            {
                Need(SideScrollingCollision.IsPositionFree(enemy.Position, enemy.Size, map), $"{enemy.Type} spawned inside Fallen City collision.");
                Need(SideScrollingCollision.IsSupported(enemy.Position, enemy.Size, map), $"{enemy.Type} spawned unsupported in Fallen City.");
            }
        }
    }

    private static void ValidateAlarmAndCollapse(DungeonMap map)
    {
        var manager = new EnemyManager(3304);
        manager.Reset(map, 4, 1, RegionType.RuinedCity, suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector();
        director.Reset(map);
        EncounterZone watch = map.EncounterZones[3];
        Vector2 player = new(watch.ActivationBounds.Left + 3, watch.ActivationBounds.Center.Y);
        for (int frame = 0; frame < 40; frame++) director.Update(Frame, player, map, manager, 4, 1);
        var alarm = new CityAlarmSystem();
        alarm.Update(Frame, map, player, manager, director, 4, 1);
        Need(alarm.Level == 1, "Watch District did not raise City Alarm.");
        CityAlarmObjectDefinition alarmObject = map.CityAlarmObjects[0];
        Need(alarm.TryDisableNearest(alarmObject.Position, map) && alarm.Level == 0, "Disabling a City Alarm object did not reduce the alarm.");
        alarm.Reset();
        Need(alarm.Level == 0 && alarm.DisabledObjects.Count == 0 &&
            alarm.WatcherSignalFeedbackTimeRemaining == 0f,
            "City Alarm reset retained level, disabled bells, or signal feedback.");
        foreach (Enemy enemy in manager.Enemies)
            enemy.ReceiveDamage(int.MaxValue);
        var calmAlarm = new CityAlarmSystem();
        int highestCalmTestLevel = 0;
        for (int frame = 0; frame < 65; frame++)
        {
            calmAlarm.Update(
                new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.1f)),
                map,
                player,
                manager,
                director,
                4,
                1);
            highestCalmTestLevel = Math.Max(
                highestCalmTestLevel,
                calmAlarm.Level);
        }
        Need(highestCalmTestLevel == 1 && calmAlarm.Level == 0,
            "City Alarm did not clear after its bounded calm period.");
        var collapse = new CollapsingCitySystem();
        CollapsingStructureDefinition structure = map.CollapsingStructures[0];
        Need(collapse.TryBreakWithHeavyAttack(structure.Bounds, map) && collapse.IsBroken(structure.StructureId), "Heavy attacks could not break an optional city structure.");
        Need(collapse.RecentlyBrokenStructureId == structure.StructureId &&
            collapse.BreakFeedbackTimeRemaining > 0f,
            "City collapse did not expose its dust/shake presentation window.");
        Need(!map.IsCollapsingStructureCollisionActive(structure) &&
            SideScrollingCollision.IsPositionFree(
                structure.Bounds.Center.ToVector2(),
                new Vector2(4f, 4f),
                map),
            "Broken city structure retained invisible collision.");
        collapse.Reset(map);
        Need(map.IsCollapsingStructureCollisionActive(structure),
            "City structure collision did not restore after reset.");
    }

    private static void ValidateBossPhases()
    {
        var boss = new FallenCityEnemy(EnemyType.BrokenKing, Vector2.Zero, 4, 1, 9, true);
        Need(boss.CombatPhase == 1, "Broken King phase one missing.");
        boss.ReceiveDamage(boss.MaxHealth * 41 / 100);
        Need(boss.CombatPhase == 2, "Broken King phase two threshold failed.");
        boss.ReceiveDamage(boss.MaxHealth * 35 / 100);
        Need(boss.CombatPhase == 3, "Broken King phase three threshold failed.");
    }

    private static void ValidateStatusAndDeathCleanup(DungeonMap map)
    {
        FallenCityZoneDefinition burned = map.FallenCityZones[4];
        Vector2 playerPosition = new(
            burned.Bounds.Left + 420f,
            burned.GroundY - Player.Player.BodyHurtboxHeight / 2f);
        var player = new Player.Player(playerPosition) { DebugGodMode = true };
        var clouds = new Combat.ProjectileManager();
        for (int index = 0; index < 12; index++)
        {
            clouds.TryCreatePlagueCloud(
                playerPosition + new Vector2(index * 80f, 0f),
                map.Rooms[4].Id,
                map);
        }
        Need(clouds.PlagueClouds.Count <= Combat.ProjectileManager.MaximumPlagueClouds,
            "Plague fields exceeded their accumulation cap.");
        for (int frame = 0; frame < 110; frame++)
        {
            GameTime step = new(TimeSpan.Zero, TimeSpan.FromSeconds(.05f));
            clouds.Update(step, player, map);
            player.UpdateTimers(step);
        }
        Need(clouds.PlagueClouds.Count == 0 && !player.IsSlowed,
            "Plague field or slow persisted after its bounded duration.");

        var roots = new Combat.RootHazardManager();
        object owner = new();
        for (int index = 0; index < 8; index++)
        {
            roots.TryAddEmberHazard(
                playerPosition + new Vector2(index * 3f, 0f),
                new Vector2(90f, 22f),
                .1f,
                .3f,
                1,
                map.Rooms[4].Id,
                owner,
                isCinderZone: true,
                map);
        }
        Need(roots.Hazards.Count <= Combat.RootHazardManager.MaximumEmberHazardsPerOwner,
            "Burning ground exceeded its per-source cap.");
        for (int frame = 0; frame < 12; frame++)
            roots.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05f)), player);
        Need(roots.Hazards.Count == 0,
            "Burning ground did not clean up after its bounded duration.");

        FallenCityZoneDefinition palace = map.FallenCityZones[8];
        DungeonRoom room = map.FindRoomContaining(palace.Bounds.Center.ToVector2());
        var enemies = new EnemyManager(3310);
        enemies.Reset(map, 4, 1, RegionType.RuinedCity,
            suppressProceduralSpawns: true);
        SpawnSocket bishopSocket = new(
            "cleanup-bishop", "cleanup", SpawnSocketRole.Elite,
            new Vector2(palace.Bounds.Left + 500f, palace.GroundY),
            new Rectangle(palace.Bounds.Left + 400, palace.GroundY - 150, 200, 170),
            new[] { EnemyType.WarBishop });
        SpawnSocket guardSocket = new(
            "cleanup-guard", "cleanup", SpawnSocketRole.Elite,
            new Vector2(palace.Bounds.Left + 680f, palace.GroundY),
            new Rectangle(palace.Bounds.Left + 580, palace.GroundY - 150, 200, 170),
            new[] { EnemyType.RoyalGuard });
        Enemy bishop = enemies.SpawnAuthoredEnemy(
            EnemyType.WarBishop, bishopSocket, room, map, 4, 1);
        Enemy guard = enemies.SpawnAuthoredEnemy(
            EnemyType.RoyalGuard, guardSocket, room, map, 4, 1);
        Need(bishop != null && guard != null,
            "War Bishop cleanup fixture could not spawn.");
        guard.ApplyTemporaryBuff(18, 10, .2f);
        guard.ApplyTemporaryBuff(6, 4, .1f);
        guard.UpdateTimers(
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.25f)));
        Need(!guard.IsBuffed,
            "War Bishop buffs stacked into an unbounded duration.");
        guard.ApplyTemporaryBuff(18, 10, 4.2f);
        bishop.ReceiveDamage(int.MaxValue);
        var defeated = new List<Enemy>();
        enemies.RemoveDefeated(defeated, out _);
        Need(!guard.IsBuffed,
            "War Bishop buff survived its source's death.");
        Need(enemies.DefeatPresentations.Count == 1 &&
            enemies.DefeatPresentations[0].Type == EnemyType.WarBishop,
            "War Bishop was removed before its death presentation.");

        SpawnSocket kingSocket = new(
            "cleanup-king", "cleanup", SpawnSocketRole.Boss,
            new Vector2(map.BossRoom.Center.X, map.BossRoom.GroundY),
            map.BossRoom.Bounds,
            new[] { EnemyType.BrokenKing });
        var bossManager = new EnemyManager(3311);
        bossManager.Reset(map, 4, 1, RegionType.RuinedCity,
            suppressProceduralSpawns: true);
        Enemy managedKing = bossManager.SpawnAuthoredEnemy(
            EnemyType.BrokenKing, kingSocket, map.BossRoom, map, 4, 1);
        Need(managedKing != null, "Broken King cleanup fixture could not spawn.");
        managedKing.ReceiveDamage(int.MaxValue);
        defeated.Clear();
        bossManager.RemoveDefeated(defeated, out _);
        Need(bossManager.DefeatPresentations.Count == 1 &&
            bossManager.DefeatPresentations[0].Type == EnemyType.BrokenKing,
            "Broken King was removed before his death presentation.");
    }

    private static void ValidateMapTwoTransition()
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 3305);
        session.Update(Frame, new KeyboardState(Keys.Enter), new MouseState());
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 2, 0);
        SettleEntry(session);
        EncounterZone final = session.CurrentDungeon.EncounterZones[^1];
        session.Player.MoveTo(new Vector2(final.ActivationBounds.Left + 4, session.CurrentDungeon.BossRoom.GroundY - 28));
        for (int frame = 0; frame < 40; frame++) session.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05)), new KeyboardState(), new MouseState());
        Enemy fallen = null;
        foreach (Enemy enemy in session.Enemies.Enemies) if (enemy.Type == EnemyType.FallenKnight) fallen = enemy;
        Need(fallen != null, "Fallen Knight did not spawn for Map 2 transition test.");
        fallen.ReceiveDamage(int.MaxValue);
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Map02Cleared && session.BossDefeated, "Map 2 boss defeat did not unlock Map 3.");
        session.Player.MoveTo(session.ExitPosition);
        session.Update(Frame, new KeyboardState(Keys.E), new MouseState());
        Need(session.DungeonDepth == 3 && session.CurrentDungeon.IsFallenCity && session.CurrentRegion == RegionType.RuinedCity, "Map 2 exit did not load Fallen City.");
        SettleEntry(session);

        EncounterZone throne = session.CurrentDungeon.EncounterZones[^1];
        session.Player.MoveTo(new Vector2(throne.ActivationBounds.Left + 4,
            session.CurrentDungeon.BossRoom.GroundY - 28));
        for (int frame = 0; frame < 40; frame++)
            session.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05)),
                new KeyboardState(), new MouseState());
        Enemy king = null;
        foreach (Enemy enemy in session.Enemies.Enemies)
            if (enemy.Type == EnemyType.BrokenKing) king = enemy;
        Need(king != null, "Broken King did not spawn in the throne plaza.");
        king.ReceiveDamage(int.MaxValue);
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Map03Cleared && session.BossDefeated,
            "Broken King defeat did not complete Map 3.");
        session.Player.MoveTo(session.ExitPosition);
        session.Update(Frame, new KeyboardState(Keys.E), new MouseState());
        Need(session.DungeonDepth == 3 && session.Map03ExitPrepared,
            "Map 3 exit did not stop at the future Map 4 hook.");
        session.ExecuteDeveloperCommand(
            DeveloperCommand.ResetCurrentBoss, 3, 9);
        Need(!session.Map03Cleared && !session.BossDefeated &&
            !session.Map03ExitPrepared,
            "Admin reset did not restore the Broken King foundation.");
    }

    private static void ValidateDeathRestartCleanup()
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 3312);
        session.Update(Frame, new KeyboardState(Keys.Enter), new MouseState());
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 3, 0);
        SettleEntry(session);

        CollapsingStructureDefinition structure =
            session.CurrentDungeon.CollapsingStructures[0];
        session.CollapsingCity.TryBreakWithHeavyAttack(
            structure.Bounds,
            session.CurrentDungeon);
        session.Projectiles.TryCreatePlagueCloud(
            session.Player.Position,
            session.CurrentDungeon.StartRoom.Id,
            session.CurrentDungeon);
        session.RootHazards.TryAddEmberHazard(
            session.Player.Position,
            new Vector2(90f, 22f),
            .1f,
            1f,
            1,
            session.CurrentDungeon.StartRoom.Id,
            session,
            isCinderZone: true,
            dungeon: session.CurrentDungeon);
        session.Player.ApplySlow(.5f, 5f);
        session.Player.DebugGodMode = false;
        session.Player.ReceiveDamage(int.MaxValue);
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.State == GameState.GameOver,
            "Map 3 Player death did not enter Game Over.");
        session.Update(Frame, new KeyboardState(Keys.R), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.State == GameState.Playing && session.DungeonDepth == 1,
            "Game Over restart did not begin a clean run.");
        Need(session.CityAlarm.Level == 0 &&
            session.CollapsingCity.BrokenStructureIds.Count == 0 &&
            session.Projectiles.Projectiles.Count == 0 &&
            session.Projectiles.PlagueClouds.Count == 0 &&
            session.RootHazards.Hazards.Count == 0 &&
            !session.Player.IsSlowed && !session.Map03ExitPrepared,
            "Game Over restart retained Map 3 transient state.");
    }

    private static void SettleEntry(GameSession session)
    {
        for (int frame = 0; frame < 100 && session.IsMapEntryFallActive; frame++) session.Update(Frame, new KeyboardState(), new MouseState());
        Need(!session.IsMapEntryFallActive && session.Player.IsGrounded, "Map entry did not settle on safe ground.");
    }

    private static int MandatoryTop(DungeonMap map, int x)
    {
        int top = int.MaxValue;
        foreach (Platform platform in map.Platforms)
            if (platform.Kind is PlatformKind.Ground or PlatformKind.Transition && x >= platform.Bounds.Left && x < platform.Bounds.Right) top = Math.Min(top, platform.Bounds.Top);
        return top;
    }

    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
