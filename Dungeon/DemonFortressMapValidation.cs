using System;
using System.Collections.Generic;
using System.Linq;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Audio;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Dungeon;

public static class DemonFortressMapValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonFortressMapGenerator().Generate();
        Need(map.IsDemonFortress && map.DemonFortressZones.Count == 9,
            "Map 5 must contain the nine authored fortress sectors.");
        Need(map.WorldBounds.Width == DemonFortressMapGenerator.WorldWidth &&
            map.WorldBounds.Width > DemonLandsMapGenerator.WorldWidth,
            "Map 5 must be larger than Map 4.");
        Need(map.StartRoom != null && map.TreasureRoom != null &&
            map.BossRoom != null && map.ExitRoom != null &&
            map.BossRoom != map.ExitRoom,
            "Map 5 room roles are incomplete.");
        Need(map.EncounterZones.Count == 8 && map.SpawnProfiles.Count == 9 &&
            map.SpawnSockets.Count >= 22,
            "Map 5 encounter/socket foundation is incomplete.");
        Need(map.FortressCheckpoints.Count == 5 && map.FortressProps.Count >= 30,
            "Map 5 checkpoint or environment metadata is incomplete.");

        ValidateConnections(map);
        ValidateContinuousRoute(map);
        ValidatePlayerTraversal(map);
        ValidateSocketsAndRoster(map);
        ValidateBossTierSpaces(map);
        ValidateCheckpointsAndExit(map);
        ValidateSessionIntegration();
        ValidateLegionnaireAndArcherDetail(map);
        ValidateHornGuardAndShieldbearerDetail(map);
        ValidatePikemanAndWarHoundDetail(map);
        ValidateSmithAndOfficerDetail(map);
        ValidateBallistaAndExecutionerDetail(map);
    }

    private static void ValidateConnections(DungeonMap map)
    {
        for (int index = 0; index < map.Rooms.Count - 1; index++)
        {
            Need(map.Rooms[index].Connections.Contains(map.Rooms[index + 1].Id) &&
                map.Rooms[index + 1].Connections.Contains(map.Rooms[index].Id),
                $"Map 5 rooms {index} and {index + 1} are not connected both ways.");
        }
    }

    private static void ValidateContinuousRoute(DungeonMap map)
    {
        int prior = int.MaxValue;
        for (int x = map.WorldBounds.Left + 4; x < map.WorldBounds.Right - 4; x += 8)
        {
            int top = MandatoryTop(map, x);
            Need(top != int.MaxValue, $"Map 5 main route has a gap near X={x}.");
            if (prior != int.MaxValue)
                Need(Math.Abs(top - prior) <= 24,
                    $"Map 5 main route has an unsafe step near X={x}.");
            prior = top;
        }
    }

    private static void ValidatePlayerTraversal(DungeonMap map)
    {
        Vector2 size = new(
            Player.Player.BodyHurtboxWidth,
            Player.Player.BodyHurtboxHeight);
        Vector2 start = SideScrollingCollision.ResolveSafeEntrySpawn(
            map.StartRoom.Bounds.Left + 105f, size, map, 0f);
        var player = new Player.Player(start);
        float target = map.ExitRoom.Bounds.Right - 155f;
        float priorX = player.Position.X;
        int stillFrames = 0;

        for (int frame = 0; frame < 10000 && player.Position.X < target; frame++)
        {
            bool jump = stillFrames >= 8 && player.IsGrounded;
            player.UpdateSideScrollingMovement(
                Frame, new KeyboardState(Keys.D), jump, map);
            stillFrames = player.Position.X <= priorX + .05f ? stillFrames + 1 : 0;
            priorX = player.Position.X;
        }

        Need(player.Position.X >= target,
            "Player A/D + Space simulation could not traverse Map 5.");
        Need(map.WorldBounds.Contains(player.Bounds),
            "Map 5 traversal left the world bounds.");
    }

    private static void ValidateSocketsAndRoster(DungeonMap map)
    {
        var roster = new HashSet<EnemyType>();
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            Need(socket.SuggestedEnemyTypes.Count == 1,
                $"{socket.SocketId} must have one deliberate authored role.");
            EnemyType type = socket.SuggestedEnemyTypes[0];
            roster.Add(type);
            DungeonRoom room = map.FindRoomContaining(socket.Position);
            Need(room != null, $"{socket.SocketId} is outside a room.");
            var manager = new EnemyManager(5100 + room.Id);
            Enemy enemy = manager.SpawnAuthoredEnemy(type, socket, room, map, 7, 3);
            Need(enemy is DemonFortressEnemy &&
                SideScrollingCollision.IsPositionFree(enemy.Position, enemy.Size, map) &&
                SideScrollingCollision.IsSupported(enemy.Position, enemy.Size, map),
                $"{socket.SocketId} cannot safely spawn {type}.");
        }

        EnemyType[] required =
        {
            EnemyType.DemonLegionnaireFortress, EnemyType.FortressArcher,
            EnemyType.HornGuard, EnemyType.DemonShieldbearer,
            EnemyType.InfernalPikeman, EnemyType.WarHound,
            EnemyType.InfernalSmith, EnemyType.DemonOfficer,
            EnemyType.BallistaGunner, EnemyType.BlackGuardExecutioner
        };
        Need(roster.Count == required.Length,
            "Map 5 sockets contain an unexpected enemy type.");
        foreach (EnemyType type in required)
        {
            Need(roster.Contains(type), $"Map 5 omits {type}.");
            Enemy enemy = EnemyFactory.Create(type, new EnemySpawnContext(
                Vector2.Zero, 7, 3, 0, false, GoblinVariant.Normal));
            Need(enemy is DemonFortressEnemy && enemy.MaxHealth > 0 &&
                enemy.AttackDamage > 0,
                $"{type} factory/runtime foundation failed.");
            enemy.ReceiveDamage(int.MaxValue);
            Need(!enemy.IsAlive, $"{type} does not complete basic death state.");
        }
    }

    private static void ValidateBossTierSpaces(DungeonMap map)
    {
        ValidateDedicatedBoss(map, EnemyType.BallistaGunner, "fortress-07");
        ValidateDedicatedBoss(map, EnemyType.BlackGuardExecutioner, "fortress-08");

        foreach (DemonFortressZoneDefinition zone in map.DemonFortressZones)
        {
            if (zone.Kind is not (DemonFortressZoneKind.BallistaBastion or
                DemonFortressZoneKind.ExecutionCourt))
                continue;
            DungeonRoom room = map.FindRoomContaining(zone.Bounds.Center.ToVector2());
            foreach (Platform platform in room.Platforms)
                Need(platform.Kind != PlatformKind.Raised,
                    $"{zone.ZoneId} boss-tier arena must retain clear vertical space.");
        }
    }

    private static void ValidateDedicatedBoss(
        DungeonMap map, EnemyType type, string sectionId)
    {
        int count = 0;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            if (!socket.SuggestedEnemyTypes.Contains(type))
                continue;
            count++;
            Need(socket.Role == SpawnSocketRole.Boss,
                $"{type} must use a boss socket.");
            EncounterZone encounter = FindEncounter(map, socket.ZoneId);
            Need(encounter != null && encounter.IsBossZone &&
                encounter.SectionId == sectionId && encounter.MaxConcurrentEnemies == 1,
                $"{type} is not isolated in its dedicated boss-tier arena.");
        }
        Need(count == 1, $"{type} must have exactly one authored socket.");
    }

    private static void ValidateCheckpointsAndExit(DungeonMap map)
    {
        foreach (FortressCheckpointDefinition checkpoint in map.FortressCheckpoints)
        {
            Need(map.WorldBounds.Contains(checkpoint.Position.ToPoint()),
                $"{checkpoint.CheckpointId} is outside Map 5.");
            Need(SideScrollingCollision.IsPositionFree(
                    checkpoint.Position, new Vector2(40f, 56f), map) &&
                SideScrollingCollision.IsSupported(
                    checkpoint.Position, new Vector2(40f, 56f), map),
                $"{checkpoint.CheckpointId} is not a safe checkpoint landmark.");
        }

        Need(map.AuthoredExitPosition.HasValue &&
            map.ExitRoom.Bounds.Contains(map.AuthoredExitPosition.Value.ToPoint()),
            "Map 5 sealed ascent lacks an authored exit position.");
        bool hasSealedGate = false;
        foreach (FortressPropDefinition prop in map.FortressProps)
            hasSealedGate |= prop.Kind == FortressPropKind.SealedGate &&
                prop.ZoneId == "fortress-09";
        Need(hasSealedGate, "Map 5 exit is not visibly prepared and sealed.");
    }

    private static void ValidateSessionIntegration()
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 5510);
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 5, 0);
        Need(session.DungeonDepth == 5 && session.CurrentDungeon.IsDemonFortress &&
            session.CurrentRegion == RegionType.DemonFortress &&
            session.CurrentMapName.Length > 0 && !session.ShouldRenderBoss,
            "Map 5 GameSession/developer-load integration failed.");

        for (int zone = 0; zone < session.CurrentDungeon.DemonFortressZones.Count; zone++)
        {
            session.ExecuteDeveloperCommand(DeveloperCommand.TeleportZone, 5, zone);
            Need(session.GetCurrentZoneIndex() == zone,
                $"Map 5 developer teleport selected the wrong zone {zone}.");
            Need(SideScrollingCollision.IsPositionFree(
                    session.Player.Position, session.Player.Size, session.CurrentDungeon) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Player.Bounds) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
                $"Map 5 zone {zone} has an unsafe player/camera arrival.");
        }

        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportZone, 5, 0);
        EncounterZone first = session.CurrentDungeon.EncounterZones[0];
        session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
            first.ActivationBounds.Left + 3f,
            session.Player.Size,
            session.CurrentDungeon,
            0f));
        for (int frame = 0; frame < 50; frame++)
        {
            session.Update(
                new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05f)),
                new KeyboardState(),
                new MouseState());
        }
        Need(session.WildForestEncounters.IsTriggered(first.ZoneId) &&
            session.Enemies.Enemies.Count > 0,
            "Map 5 encounter director did not spawn authored enemies.");
    }

    private static void ValidateLegionnaireAndArcherDetail(DungeonMap map)
    {
        Need(AudioProfiles.GetEnemy(EnemyType.DemonLegionnaireFortress) != null &&
            AudioProfiles.GetEnemy(EnemyType.FortressArcher) != null,
            "Map 5 enemies 41/42 need valid audio profiles.");

        DungeonRoom arena = map.Rooms[6];
        Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena);
        var player = new Player.Player(playerPosition) { DebugGodMode = true };
        var projectiles = new ProjectileManager();

        var legion = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.DemonLegionnaireFortress,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 76f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        legion.SnapToGround(arena);
        bool sawLegionWindup = false;
        bool sawLegionActive = false;
        bool sawLegionRecovery = false;
        int longestLegionState = 0;
        int sameLegionState = 0;
        DemonFortressCombatState priorLegion = legion.CombatState;
        for (int frame = 0; frame < 260; frame++)
        {
            legion.Update(Frame, player, map, projectiles);
            sawLegionWindup |= legion.CombatState is
                DemonFortressCombatState.LegionSlashWindup or
                DemonFortressCombatState.LegionFollowupWindup;
            sawLegionActive |= legion.CombatState is
                DemonFortressCombatState.LegionSlashActive or
                DemonFortressCombatState.LegionFollowupActive;
            sawLegionRecovery |= legion.CombatState is
                DemonFortressCombatState.LegionSlashRecovery or
                DemonFortressCombatState.LegionFollowupRecovery;
            sameLegionState = legion.CombatState == priorLegion
                ? sameLegionState + 1 : 0;
            longestLegionState = Math.Max(longestLegionState, sameLegionState);
            priorLegion = legion.CombatState;
        }
        Need(sawLegionWindup && sawLegionActive && sawLegionRecovery &&
            longestLegionState < 80,
            "Demon Legionnaire attack states do not enter and exit cleanly.");

        var guardingLegion = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.DemonLegionnaireFortress,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 200f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        guardingLegion.SnapToGround(arena);
        bool sawFiniteGuard = false;
        bool verifiedFrontalBlock = false;
        int consecutiveGuardFrames = 0;
        int longestGuard = 0;
        for (int frame = 0; frame < 220; frame++)
        {
            guardingLegion.Update(Frame, player, map, projectiles);
            if (guardingLegion.IsLegionGuarding)
            {
                sawFiniteGuard = true;
                consecutiveGuardFrames++;
                longestGuard = Math.Max(longestGuard, consecutiveGuardFrames);
                if (!verifiedFrontalBlock)
                {
                    int healthBefore = guardingLegion.CurrentHealth;
                    guardingLegion.ReceiveDamageFrom(20, 10f, player.Position);
                    int damageTaken = healthBefore - guardingLegion.CurrentHealth;
                    verifiedFrontalBlock = damageTaken > 0 && damageTaken < 20;
                }
            }
            else
                consecutiveGuardFrames = 0;
        }
        Need(sawFiniteGuard && verifiedFrontalBlock && longestGuard < 60,
            "Demon Legionnaire guard is not finite or does not protect its front.");

        var archer = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.FortressArcher,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 360f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        archer.SnapToGround(arena);
        projectiles.Clear();
        bool sawAim = false;
        bool sawDraw = false;
        bool sawHold = false;
        bool sawRelease = false;
        bool sawRecovery = false;
        bool sawProjectile = false;
        int longestArcherState = 0;
        int sameArcherState = 0;
        DemonFortressCombatState priorArcher = archer.CombatState;
        for (int frame = 0; frame < 260; frame++)
        {
            archer.Update(Frame, player, map, projectiles);
            sawAim |= archer.CombatState == DemonFortressCombatState.ArcherAim;
            sawDraw |= archer.CombatState == DemonFortressCombatState.ArcherDraw;
            sawHold |= archer.CombatState == DemonFortressCombatState.ArcherHold;
            sawRelease |= archer.CombatState == DemonFortressCombatState.ArcherRelease;
            sawRecovery |= archer.CombatState == DemonFortressCombatState.ArcherRecovery;
            sawProjectile |= projectiles.Projectiles.Count > 0;
            projectiles.Update(Frame, player, map);
            sameArcherState = archer.CombatState == priorArcher
                ? sameArcherState + 1 : 0;
            longestArcherState = Math.Max(longestArcherState, sameArcherState);
            priorArcher = archer.CombatState;
        }
        Need(sawAim && sawDraw && sawHold && sawRelease && sawRecovery &&
            sawProjectile && longestArcherState < 80,
            "Fortress Archer aim/release states do not enter and exit cleanly.");

        DungeonRoom forge = map.Rooms[3];
        Platform blocker = null;
        foreach (Platform platform in forge.Platforms)
        {
            if (platform.Kind == PlatformKind.Raised)
            {
                blocker = platform;
                break;
            }
        }
        Need(blocker != null, "Map 5 LOS fixture lacks a raised solid platform.");
        var blockedArcher = new DemonFortressEnemy(
            EnemyType.FortressArcher,
            new Vector2(blocker.Bounds.Left - 90f, blocker.Bounds.Center.Y),
            7, 3, forge.Id, false);
        Vector2 blockedTarget = new(blocker.Bounds.Right + 90f, blocker.Bounds.Center.Y);
        Need(!blockedArcher.HasClearShot(blockedTarget, map),
            "Fortress Archer line-of-sight ignores solid platform geometry.");

        projectiles.Clear();
        projectiles.SpawnEnemyArrow(
            new Vector2(blocker.Bounds.Left - 24f, blocker.Bounds.Center.Y),
            Vector2.UnitX,
            1,
            forge.Id,
            heavy: false);
        var distantPlayer = new Player.Player(
            new Vector2(blocker.Bounds.Right + 300f, blocker.Bounds.Center.Y))
        {
            DebugGodMode = true
        };
        for (int frame = 0; frame < 24 && projectiles.Projectiles.Count > 0; frame++)
            projectiles.Update(Frame, distantPlayer, map);
        Need(projectiles.Projectiles.Count == 0,
            "Fortress Archer projectile did not resolve against terrain.");

        legion.ReceiveDamage(int.MaxValue);
        archer.ReceiveDamage(int.MaxValue);
        Need(!legion.IsAlive && !archer.IsAlive,
            "Map 5 enemies 41/42 do not enter their death presentation.");
    }

    private static void ValidateHornGuardAndShieldbearerDetail(DungeonMap map)
    {
        Need(AudioProfiles.GetEnemy(EnemyType.HornGuard) != null &&
            AudioProfiles.GetEnemy(EnemyType.DemonShieldbearer) != null,
            "Map 5 enemies 43/44 need valid audio profiles.");

        DungeonRoom arena = map.Rooms[6];
        Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena);
        var player = new Player.Player(playerPosition) { DebugGodMode = true };
        var projectiles = new ProjectileManager();

        var horn = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.HornGuard,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 320f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        horn.SnapToGround(arena);
        int alarmEvents = 0;
        horn.AlarmCompleted += _ => alarmEvents++;
        bool sawHornWindup = false;
        bool sawHornBlast = false;
        bool sawHornRecovery = false;
        for (int frame = 0; frame < 210; frame++)
        {
            horn.Update(Frame, player, map, projectiles);
            sawHornWindup |= horn.IsHornAlarmWindup;
            sawHornBlast |= horn.IsHornAlarmCompleted;
            sawHornRecovery |= horn.CombatState ==
                DemonFortressCombatState.HornRecovery;
        }
        Need(sawHornWindup && sawHornBlast && sawHornRecovery &&
            alarmEvents == 1 && horn.AlarmCompletionCount == 1,
            "Horn Guard alarm does not complete through its readable state flow.");
        for (int frame = 0; frame < 240; frame++)
            horn.Update(Frame, player, map, projectiles);
        Need(alarmEvents == 1,
            "Horn Guard alarm cooldown permits immediate repeated alarms.");

        var interruptedHorn = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.HornGuard,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 320f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        interruptedHorn.SnapToGround(arena);
        int interruptedEvents = 0;
        interruptedHorn.AlarmCompleted += _ => interruptedEvents++;
        for (int frame = 0; frame < 90 && !interruptedHorn.IsHornAlarmWindup; frame++)
            interruptedHorn.Update(Frame, player, map, projectiles);
        Need(interruptedHorn.IsHornAlarmWindup,
            "Horn Guard did not begin an interruptible alarm windup.");
        interruptedHorn.ReceiveDamageFrom(1, 0f, player.Position);
        Need(interruptedHorn.IsHornAlarmInterrupted,
            "A valid hit did not interrupt the Horn Guard alarm.");
        for (int frame = 0; frame < 240; frame++)
            interruptedHorn.Update(Frame, player, map, projectiles);
        Need(interruptedEvents == 0,
            "Interrupted Horn Guard retried its alarm before cooldown elapsed.");

        var defeatedHorn = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.HornGuard,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 320f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        defeatedHorn.SnapToGround(arena);
        int defeatedAlarmEvents = 0;
        defeatedHorn.AlarmCompleted += _ => defeatedAlarmEvents++;
        for (int frame = 0; frame < 90 && !defeatedHorn.IsHornAlarmWindup; frame++)
            defeatedHorn.Update(Frame, player, map, projectiles);
        defeatedHorn.ReceiveDamage(int.MaxValue);
        for (int frame = 0; frame < 150; frame++)
            defeatedHorn.Update(Frame, player, map, projectiles);
        Need(!defeatedHorn.IsAlive && defeatedAlarmEvents == 0,
            "Defeated Horn Guard completed a pending alarm.");

        var retreatingHorn = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.HornGuard,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 120f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        retreatingHorn.SnapToGround(arena);
        bool sawRetreat = false;
        bool exitedRetreat = false;
        for (int frame = 0; frame < 120; frame++)
        {
            retreatingHorn.Update(Frame, player, map, projectiles);
            if (retreatingHorn.CombatState == DemonFortressCombatState.HornRetreat)
                sawRetreat = true;
            else if (sawRetreat)
                exitedRetreat = true;
        }
        Need(sawRetreat && exitedRetreat,
            "Horn Guard emergency retreat does not terminate.");

        var shield = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.DemonShieldbearer,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 200f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        shield.SnapToGround(arena);
        bool sawGuard = false;
        bool checkedGuardDirections = false;
        int guardFrames = 0;
        int longestGuard = 0;
        for (int frame = 0; frame < 260; frame++)
        {
            shield.Update(Frame, player, map, projectiles);
            if (shield.IsShieldbearerGuarding)
            {
                sawGuard = true;
                guardFrames++;
                longestGuard = Math.Max(longestGuard, guardFrames);
                if (!checkedGuardDirections)
                {
                    int beforeFront = shield.CurrentHealth;
                    shield.ReceiveDamageFrom(20, 8f, player.Position);
                    int frontDamage = beforeFront - shield.CurrentHealth;
                    int beforeRear = shield.CurrentHealth;
                    shield.ReceiveDamageFrom(20, 8f,
                        shield.Position + new Vector2(-100f, 0f));
                    int rearDamage = beforeRear - shield.CurrentHealth;
                    checkedGuardDirections = frontDamage > 0 && frontDamage < rearDamage &&
                        rearDamage == 20;
                }
            }
            else
                guardFrames = 0;
        }
        Need(sawGuard && checkedGuardDirections && longestGuard < 70,
            "Shieldbearer guard is permanent or does not preserve rear vulnerability.");

        var pushingShield = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.DemonShieldbearer,
            new EnemySpawnContext(
                new Vector2(player.Position.X - 68f, player.Position.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        pushingShield.SnapToGround(arena);
        bool sawPushWindup = false;
        bool sawPushActive = false;
        bool sawPushRecovery = false;
        for (int frame = 0; frame < 120; frame++)
        {
            pushingShield.Update(Frame, player, map, projectiles);
            sawPushWindup |= pushingShield.CombatState ==
                DemonFortressCombatState.ShieldPushWindup;
            sawPushActive |= pushingShield.CombatState ==
                DemonFortressCombatState.ShieldPushActive;
            sawPushRecovery |= pushingShield.CombatState ==
                DemonFortressCombatState.ShieldPushRecovery;
        }
        Need(sawPushWindup && sawPushActive && sawPushRecovery,
            "Shieldbearer Shield Push states do not enter and exit cleanly.");

        shield.ReceiveDamage(int.MaxValue);
        Need(!shield.IsAlive && !shield.IsShieldbearerGuarding &&
            shield.CombatState == DemonFortressCombatState.Ready,
            "Defeated Shieldbearer retained an active guard state.");
    }

    private static void ValidatePikemanAndWarHoundDetail(DungeonMap map)
    {
        Need(AudioProfiles.GetEnemy(EnemyType.InfernalPikeman) != null &&
            AudioProfiles.GetEnemy(EnemyType.WarHound) != null,
            "Map 5 enemies 45/46 need valid audio profiles.");

        DungeonRoom arena = map.Rooms[6];
        Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena);
        var player = new Player.Player(playerPosition) { DebugGodMode = true };
        var projectiles = new ProjectileManager();

        var pikeman = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.InfernalPikeman,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 150f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        pikeman.SnapToGround(arena);
        bool sawSettle = false;
        bool sawExtend = false;
        bool sawHold = false;
        bool sawRetract = false;
        bool sawRecovery = false;
        int earliestTip = int.MaxValue;
        int latestTip = int.MinValue;
        int longestPikeState = 0;
        int samePikeState = 0;
        DemonFortressCombatState priorPike = pikeman.CombatState;
        for (int frame = 0; frame < 240; frame++)
        {
            pikeman.Update(Frame, player, map, projectiles);
            sawSettle |= pikeman.CombatState == DemonFortressCombatState.PikeSettle;
            sawExtend |= pikeman.CombatState == DemonFortressCombatState.PikeThrustExtend;
            sawHold |= pikeman.CombatState == DemonFortressCombatState.PikeThrustHold;
            sawRetract |= pikeman.CombatState == DemonFortressCombatState.PikeRetract;
            sawRecovery |= pikeman.CombatState == DemonFortressCombatState.PikeRecovery;
            if (pikeman.CombatState == DemonFortressCombatState.PikeThrustExtend)
            {
                earliestTip = Math.Min(earliestTip, pikeman.ActiveAttackArea.Right);
                latestTip = Math.Max(latestTip, pikeman.ActiveAttackArea.Right);
            }
            samePikeState = pikeman.CombatState == priorPike
                ? samePikeState + 1 : 0;
            longestPikeState = Math.Max(longestPikeState, samePikeState);
            priorPike = pikeman.CombatState;
        }
        Need(sawSettle && sawExtend && sawHold && sawRetract && sawRecovery &&
            latestTip - earliestTip >= 70 && longestPikeState < 80,
            "Infernal Pikeman thrust extension or recovery flow is invalid.");

        var closePikeman = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.InfernalPikeman,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 70f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        closePikeman.SnapToGround(arena);
        bool sawPikeRetreat = false;
        bool exitedPikeRetreat = false;
        for (int frame = 0; frame < 120; frame++)
        {
            closePikeman.Update(Frame, player, map, projectiles);
            if (closePikeman.CombatState == DemonFortressCombatState.PikeReposition)
                sawPikeRetreat = true;
            else if (sawPikeRetreat)
                exitedPikeRetreat = true;
        }
        Need(sawPikeRetreat && exitedPikeRetreat,
            "Infernal Pikeman close-range reposition does not terminate.");

        var advancingPikeman = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.InfernalPikeman,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 195f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        advancingPikeman.SnapToGround(arena);
        bool sawAdvance = false;
        bool sawAdvanceRecovery = false;
        for (int frame = 0; frame < 180; frame++)
        {
            advancingPikeman.Update(Frame, player, map, projectiles);
            sawAdvance |= advancingPikeman.CombatState is
                DemonFortressCombatState.PikeAdvanceExtend or
                DemonFortressCombatState.PikeAdvanceHold;
            sawAdvanceRecovery |= advancingPikeman.CombatState ==
                DemonFortressCombatState.PikeAdvanceRecovery;
        }
        Need(sawAdvance && sawAdvanceRecovery,
            "Infernal Pikeman advancing thrust does not reach recovery.");

        var houndPlayer = new Player.Player(playerPosition) { DebugGodMode = true };
        var hound = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.WarHound,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 190f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        hound.SnapToGround(arena);
        bool sawLungeWindup = false;
        bool sawLungeActive = false;
        bool sawLanding = false;
        bool sawLungeRecovery = false;
        bool verifiedLockedDirection = false;
        int lungeStarts = 0;
        DemonFortressCombatState priorHound = hound.CombatState;
        for (int frame = 0; frame < 180; frame++)
        {
            float beforeX = hound.Position.X;
            hound.Update(Frame, houndPlayer, map, projectiles);
            if (hound.CombatState == DemonFortressCombatState.HoundLungeWindup &&
                priorHound != hound.CombatState)
                lungeStarts++;
            sawLungeWindup |= hound.CombatState ==
                DemonFortressCombatState.HoundLungeWindup;
            sawLungeActive |= hound.CombatState ==
                DemonFortressCombatState.HoundLungeActive;
            sawLanding |= hound.CombatState == DemonFortressCombatState.HoundLanding;
            sawLungeRecovery |= hound.CombatState ==
                DemonFortressCombatState.HoundLungeRecovery;
            if (!verifiedLockedDirection &&
                hound.CombatState == DemonFortressCombatState.HoundLungeActive)
            {
                houndPlayer.MoveTo(hound.Position - new Vector2(140f, 0f));
                float launchX = hound.Position.X;
                hound.Update(Frame, houndPlayer, map, projectiles);
                verifiedLockedDirection = hound.Position.X > launchX &&
                    hound.Position.X >= beforeX;
            }
            priorHound = hound.CombatState;
        }
        Need(sawLungeWindup && sawLungeActive && sawLanding && sawLungeRecovery &&
            verifiedLockedDirection && lungeStarts == 1,
            "War Hound lunge does not commit, miss, and recover cleanly.");

        var bitingHound = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.WarHound,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 55f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        bitingHound.SnapToGround(arena);
        bool sawBiteActive = false;
        bool sawBiteRecovery = false;
        int consecutiveBiteFrames = 0;
        int longestBite = 0;
        for (int frame = 0; frame < 150; frame++)
        {
            bitingHound.Update(Frame, player, map, projectiles);
            if (bitingHound.CombatState == DemonFortressCombatState.HoundBiteActive)
            {
                sawBiteActive = true;
                consecutiveBiteFrames++;
                longestBite = Math.Max(longestBite, consecutiveBiteFrames);
            }
            else
                consecutiveBiteFrames = 0;
            sawBiteRecovery |= bitingHound.CombatState ==
                DemonFortressCombatState.HoundBiteRecovery;
        }
        Need(sawBiteActive && sawBiteRecovery && longestBite < 15,
            "War Hound bite lacks a bounded damage window and recovery.");

        for (int frame = 0;
            frame < 180 && pikeman.IsAlive && !pikeman.IsCombatAttackActive;
            frame++)
            pikeman.Update(Frame, player, map, projectiles);
        Need(pikeman.IsCombatAttackActive,
            "Infernal Pikeman could not re-enter an active thrust for cleanup validation.");
        pikeman.ReceiveDamage(int.MaxValue);
        hound.ReceiveDamage(int.MaxValue);
        Need(!pikeman.IsAlive && !pikeman.IsCombatAttackActive &&
            pikeman.CombatState == DemonFortressCombatState.Ready &&
            !hound.IsAlive && !hound.IsCombatAttackActive &&
            hound.CombatState == DemonFortressCombatState.Ready,
            "Map 5 enemies 45/46 retained active attacks after death.");
    }

    private static void ValidateSmithAndOfficerDetail(DungeonMap map)
    {
        Need(AudioProfiles.GetEnemy(EnemyType.InfernalSmith) != null &&
            AudioProfiles.GetEnemy(EnemyType.DemonOfficer) != null,
            "Map 5 enemies 47/48 need valid audio profiles.");

        DungeonRoom arena = map.Rooms[6];
        Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena);
        var player = new Player.Player(playerPosition) { DebugGodMode = true };
        var projectiles = new ProjectileManager();

        var smith = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.InfernalSmith,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 90f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        smith.SnapToGround(arena);
        bool sawSlamRaise = false;
        bool sawSlamImpact = false;
        bool sawSlamRecovery = false;
        bool sawSwing = false;
        bool sawSwingRecovery = false;
        int impactFrames = 0;
        int longestImpact = 0;
        for (int frame = 0; frame < 360; frame++)
        {
            smith.Update(Frame, player, map, projectiles);
            sawSlamRaise |= smith.CombatState ==
                DemonFortressCombatState.SmithSlamRaise;
            sawSlamImpact |= smith.IsSmithGroundImpact;
            sawSlamRecovery |= smith.CombatState ==
                DemonFortressCombatState.SmithSlamRecovery;
            sawSwing |= smith.CombatState == DemonFortressCombatState.SmithSwingActive;
            sawSwingRecovery |= smith.CombatState ==
                DemonFortressCombatState.SmithSwingRecovery;
            if (smith.IsSmithGroundImpact)
            {
                impactFrames++;
                longestImpact = Math.Max(longestImpact, impactFrames);
            }
            else
                impactFrames = 0;
        }
        Need(sawSlamRaise && sawSlamImpact && sawSlamRecovery && sawSwing &&
            sawSwingRecovery && longestImpact < 20,
            "Infernal Smith slam/swing states or ground impact do not exit cleanly.");
        int smithHealth = smith.CurrentHealth;
        smith.ReceiveDamage(10, smith.MaxPoise + 1f);
        Need(smith.CurrentHealth < smithHealth && smith.IsStaggered,
            "Infernal Smith poise incorrectly provides permanent invulnerability.");

        var directOfficer = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.DemonOfficer,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 76f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        directOfficer.SnapToGround(arena);
        bool sawOfficerSlash = false;
        bool sawOfficerRecovery = false;
        for (int frame = 0; frame < 180; frame++)
        {
            directOfficer.Update(Frame, player, map, projectiles);
            sawOfficerSlash |= directOfficer.CombatState ==
                DemonFortressCombatState.OfficerSlashActive;
            sawOfficerRecovery |= directOfficer.CombatState ==
                DemonFortressCombatState.OfficerSlashRecovery;
        }
        Need(sawOfficerSlash && sawOfficerRecovery,
            "Demon Officer direct attack does not reach recovery.");

        var formation = new EnemyManager(5480);
        var officer = (DemonFortressEnemy)formation.SpawnWildForestShowcaseEnemy(
            EnemyType.DemonOfficer,
            arena,
            playerPosition.X - 190f,
            7,
            3);
        Enemy ally = formation.SpawnWildForestShowcaseEnemy(
            EnemyType.DemonLegionnaireFortress,
            arena,
            playerPosition.X - 125f,
            7,
            3);
        int deathCallbacks = 0;
        officer.OfficerDefeated += _ => deathCallbacks++;
        for (int frame = 0; frame < 240; frame++)
            formation.UpdateCombatAndAi(Frame, player, map);
        Need(officer.OfficerCommandCount == 1 && ally.IsBuffed &&
            officer.ActiveCommandedAllyCount == 1,
            "Demon Officer command is missing, stacking, or outside its bounded formation.");

        officer.ReceiveDamage(int.MaxValue);
        officer.ReceiveDamage(int.MaxValue);
        Need(!officer.IsAlive && deathCallbacks == 1 &&
            officer.OfficerDeathNotificationCount == 1 &&
            !ally.IsBuffed && officer.ActiveCommandedAllyCount == 0 &&
            officer.CombatState == DemonFortressCombatState.Ready,
            "Demon Officer death callback or command cleanup is invalid.");

        var activeSmith = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.InfernalSmith,
            new EnemySpawnContext(
                new Vector2(playerPosition.X - 90f, playerPosition.Y),
                7, 3, arena.Id, false, GoblinVariant.Normal));
        activeSmith.SnapToGround(arena);
        for (int frame = 0;
            frame < 180 && !activeSmith.IsCombatAttackActive;
            frame++)
            activeSmith.Update(Frame, player, map, projectiles);
        Need(activeSmith.IsCombatAttackActive,
            "Infernal Smith could not enter an active attack for cleanup validation.");
        activeSmith.ReceiveDamage(int.MaxValue);
        Need(!activeSmith.IsAlive && !activeSmith.IsCombatAttackActive &&
            activeSmith.CombatState == DemonFortressCombatState.Ready,
            "Defeated Infernal Smith retained an active hammer state.");
    }

    private static void ValidateBallistaAndExecutionerDetail(DungeonMap map)
    {
        Need(AudioProfiles.GetEnemy(EnemyType.BallistaGunner) != null &&
            AudioProfiles.GetEnemy(EnemyType.BlackGuardExecutioner) != null,
            "Map 5 boss-tier encounters need valid audio profiles.");

        var encounterDirector = new WildForestEncounterDirector();
        var encounterEnemies = new EnemyManager(5490);
        encounterDirector.Reset(map);
        EncounterZone ballistaZone = FindEncounter(
            map, "fortress-ballista-gunner");
        EncounterZone executionerZone = FindEncounter(
            map, "fortress-black-guard");
        Vector2 ballistaEntry = new(
            ballistaZone.ActivationBounds.Left + 4f,
            ballistaZone.ActivationBounds.Center.Y);
        for (int frame = 0; frame < 40; frame++)
            encounterDirector.Update(Frame, ballistaEntry, map,
                encounterEnemies, 7, 3);
        Need(encounterDirector.TryGetActiveFortressTrialBounds(
                out Rectangle activeBallistaBounds) &&
            activeBallistaBounds == ballistaZone.ActivationBounds &&
            encounterEnemies.Enemies.Count(enemy =>
                enemy.Type == EnemyType.BallistaGunner) == 1,
            "Ballista arena must seal around exactly one encounter spawn.");

        Vector2 executionerEntry = new(
            executionerZone.ActivationBounds.Left + 4f,
            executionerZone.ActivationBounds.Center.Y);
        for (int frame = 0; frame < 40; frame++)
            encounterDirector.Update(Frame, executionerEntry, map,
                encounterEnemies, 7, 3);
        Need(encounterEnemies.Enemies.Count(enemy =>
                enemy.Type == EnemyType.BlackGuardExecutioner) == 0,
            "Black Guard Trial must not activate while the Ballista arena is sealed.");
        Enemy authoredBallista = encounterEnemies.Enemies.First(enemy =>
            enemy.Type == EnemyType.BallistaGunner);
        authoredBallista.ReceiveDamage(int.MaxValue);
        encounterDirector.Update(Frame, ballistaEntry, map,
            encounterEnemies, 7, 3);
        Need(encounterDirector.IsCleared(ballistaZone.ZoneId) &&
            !encounterDirector.TryGetActiveFortressTrialBounds(out _),
            "Ballista arena gate did not open after its exact-once clear.");
        for (int frame = 0; frame < 40; frame++)
            encounterDirector.Update(Frame, executionerEntry, map,
                encounterEnemies, 7, 3);
        Need(encounterDirector.TryGetActiveFortressTrialBounds(
                out Rectangle activeTrialBounds) &&
            activeTrialBounds == executionerZone.ActivationBounds &&
            encounterEnemies.Enemies.Count(enemy =>
                enemy.Type == EnemyType.BlackGuardExecutioner) == 1,
            "Black Guard Trial must seal as a single-enemy duel after entry.");
        Enemy authoredExecutioner = encounterEnemies.Enemies.First(enemy =>
            enemy.Type == EnemyType.BlackGuardExecutioner);
        authoredExecutioner.ReceiveDamage(int.MaxValue);
        encounterDirector.Update(Frame, executionerEntry, map,
            encounterEnemies, 7, 3);
        Need(encounterDirector.IsCleared(executionerZone.ZoneId) &&
            !encounterDirector.TryGetActiveFortressTrialBounds(out _),
            "Black Guard Trial gate/completion did not resolve exactly once.");

        SpawnSocket ballistaSocket = FindSocket(map, EnemyType.BallistaGunner);
        DungeonRoom ballistaRoom = map.FindRoomContaining(ballistaSocket.Position);
        Vector2 ballistaPlayerPosition = SideScrollingCollision.PlaceOnGround(
            ballistaSocket.Position.X - 520f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            ballistaRoom);
        var ballistaPlayer = new Player.Player(ballistaPlayerPosition)
            { DebugGodMode = true };
        var ballistaProjectiles = new ProjectileManager();
        var ballista = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.BallistaGunner,
            new EnemySpawnContext(ballistaSocket.Position, 7, 3,
                ballistaRoom.Id, true, GoblinVariant.Normal));
        ballista.SnapToGround(ballistaRoom);
        int warningFrame = -1;
        int fireFrame = -1;
        bool sawAcquire = false;
        bool sawReload = false;
        bool sawReady = false;
        for (int frame = 0; frame < 520; frame++)
        {
            ballista.Update(Frame, ballistaPlayer, map, ballistaProjectiles);
            sawAcquire |= ballista.CombatState ==
                DemonFortressCombatState.BallistaAcquire;
            if (warningFrame < 0 && ballista.IsBallistaWarning)
                warningFrame = frame;
            if (fireFrame < 0 && ballista.CombatState ==
                DemonFortressCombatState.BallistaFire)
                fireFrame = frame;
            sawReload |= ballista.CombatState is
                DemonFortressCombatState.BallistaReloadReach or
                DemonFortressCombatState.BallistaReloadCrank or
                DemonFortressCombatState.BallistaReloadLoad or
                DemonFortressCombatState.BallistaReloadTension;
            sawReady |= ballista.CombatState ==
                DemonFortressCombatState.BallistaReady;
        }
        Need(sawAcquire && warningFrame >= 0 && fireFrame > warningFrame &&
            sawReload && sawReady && ballista.BallistaCycleCount > 0 &&
            ballista.BallistaShotCount <= ballista.BallistaWarningCount,
            "Ballista firing cycle lacks warning, reload, or a bounded return to ready.");
        Need(ballistaProjectiles.Projectiles.Count > 0 &&
            ballistaProjectiles.Projectiles[0].Trajectory ==
                ProjectileTrajectoryType.Straight &&
            ballistaProjectiles.Projectiles[0].Gravity == 0f &&
            ballistaProjectiles.Projectiles[0].LockedTarget == null &&
            !ballistaProjectiles.Projectiles[0].PenetratesTerrain,
            "Ballista bolt must be non-homing and terrain-bound after release.");
        int ballistaCallbacks = 0;
        ballista.BossTierDefeated += _ => ballistaCallbacks++;
        ballista.ReceiveDamage(int.MaxValue);
        ballista.ReceiveDamage(int.MaxValue);
        Need(ballistaCallbacks == 1 && ballista.BossTierCompletionCount == 1 &&
            ballistaProjectiles.Projectiles.Count == 0 && !ballista.CanDropLoot,
            "Ballista death cleanup/completion must happen exactly once without loot.");

        SpawnSocket executionerSocket = FindSocket(
            map, EnemyType.BlackGuardExecutioner);
        DungeonRoom executionerRoom = map.FindRoomContaining(
            executionerSocket.Position);
        Vector2 executionerPlayerPosition = SideScrollingCollision.PlaceOnGround(
            executionerSocket.Position.X - 98f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            executionerRoom);
        var executionerPlayer = new Player.Player(executionerPlayerPosition)
            { DebugGodMode = true };
        var executioner = (DemonFortressEnemy)EnemyFactory.Create(
            EnemyType.BlackGuardExecutioner,
            new EnemySpawnContext(executionerSocket.Position, 7, 3,
                executionerRoom.Id, true, GoblinVariant.Normal));
        executioner.SnapToGround(executionerRoom);
        bool sawCleave = false;
        bool sawArmorBreak = false;
        bool sawCounterWindow = false;
        bool sawCounterRecovery = false;
        bool counterTriggered = false;
        for (int frame = 0; frame < 1500; frame++)
        {
            executioner.Update(Frame, executionerPlayer, map,
                ballistaProjectiles);
            sawCleave |= executioner.CombatState is
                DemonFortressCombatState.ExecutionerCleaveActive or
                DemonFortressCombatState.ExecutionerCleaveRecovery;
            sawArmorBreak |= executioner.CombatState is
                DemonFortressCombatState.ExecutionerArmorBreakActive or
                DemonFortressCombatState.ExecutionerArmorBreakRecovery;
            sawCounterWindow |= executioner.CombatState ==
                DemonFortressCombatState.ExecutionerCounterWindow;
            sawCounterRecovery |= executioner.CombatState ==
                DemonFortressCombatState.ExecutionerCounterRecovery;
            if (!counterTriggered && executioner.CombatState ==
                DemonFortressCombatState.ExecutionerCounterWindow)
            {
                counterTriggered = true;
                executioner.ReceiveDamageFrom(
                    20, 5f, executionerPlayer.Position);
            }
        }
        Need(sawCleave && sawArmorBreak && sawCounterWindow &&
            sawCounterRecovery && executioner.ExecutionerCounterTriggerCount == 1,
            "Executioner phase-one attacks or bounded Counter Stance are incomplete.");

        executioner.ReceiveDamage(
            Math.Max(1, executioner.CurrentHealth - executioner.MaxHealth / 2));
        bool sawJudgment = false;
        bool sawJudgmentRecovery = false;
        for (int frame = 0; frame < 1000; frame++)
        {
            executioner.Update(Frame, executionerPlayer, map,
                ballistaProjectiles);
            sawJudgment |= executioner.CombatState is
                DemonFortressCombatState.ExecutionerJudgmentRaise or
                DemonFortressCombatState.ExecutionerJudgmentHold or
                DemonFortressCombatState.ExecutionerJudgmentFall or
                DemonFortressCombatState.ExecutionerJudgmentImpact;
            sawJudgmentRecovery |= executioner.CombatState ==
                DemonFortressCombatState.ExecutionerJudgmentRecovery;
        }
        Need(executioner.BossCombatPhase == 2 &&
            executioner.ExecutionerPhaseTransitionCount == 1 &&
            sawJudgment && sawJudgmentRecovery,
            "Executioner phase transition or Overhead Judgment recovery is invalid.");
        int healthBeforePoiseBreak = executioner.CurrentHealth;
        executioner.ReceiveDamage(10, executioner.MaxPoise + 1f);
        Need(executioner.CurrentHealth < healthBeforePoiseBreak &&
            executioner.IsStaggered,
            "Executioner poise incorrectly creates permanent super armor.");
        int executionerCallbacks = 0;
        executioner.BossTierDefeated += _ => executionerCallbacks++;
        executioner.ReceiveDamage(int.MaxValue);
        executioner.ReceiveDamage(int.MaxValue);
        Need(executionerCallbacks == 1 &&
            executioner.BossTierCompletionCount == 1 &&
            executioner.CombatState == DemonFortressCombatState.Ready &&
            !executioner.IsCombatAttackActive && !executioner.CanDropLoot,
            "Black Guard Trial completion/death cleanup must fire exactly once.");
    }

    private static SpawnSocket FindSocket(DungeonMap map, EnemyType type)
    {
        foreach (SpawnSocket socket in map.SpawnSockets)
            if (socket.SuggestedEnemyTypes.Contains(type))
                return socket;
        throw new InvalidOperationException($"Map 5 has no socket for {type}.");
    }

    private static EncounterZone FindEncounter(DungeonMap map, string id)
    {
        foreach (EncounterZone encounter in map.EncounterZones)
            if (encounter.ZoneId == id)
                return encounter;
        return null;
    }

    private static int MandatoryTop(DungeonMap map, int x)
    {
        int result = int.MaxValue;
        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Ground || x < platform.Bounds.Left ||
                x >= platform.Bounds.Right)
                continue;
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
