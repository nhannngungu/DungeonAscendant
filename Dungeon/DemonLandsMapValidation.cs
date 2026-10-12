using System;
using System.Collections.Generic;
using System.IO;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Dungeon;

public static class DemonLandsMapValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonLandsMapGenerator().Generate();
        Need(map.IsDemonLands && map.DemonLandsZones.Count == 10,
            "Map 4 needs ten Demon Lands zones.");
        Need(map.WorldBounds.Width == DemonLandsMapGenerator.WorldWidth &&
            map.StartRoom != null && map.BossRoom != null &&
            map.ExitRoom == map.BossRoom,
            "Map 4 world/start/boss/exit contract failed.");
        Need(map.EncounterZones.Count == 10 && map.SpawnProfiles.Count == 10 &&
            map.SpawnSockets.Count >= 28,
            "Map 4 encounter ecology is incomplete.");
        Need(map.CorruptionAnchors.Count >= 8 &&
            map.CorruptionFields.Count >= 10 &&
            map.PurificationShrines.Count >= 4,
            "Map 4 corruption ecology is incomplete.");
        Need(map.RouteFeatures.Count >= 8,
            "Map 4 side routes, shortcuts, and secrets are incomplete.");

        ValidateContinuousGround(map);
        ValidateActualPlayerTraversal(map);
        ValidateRoster(map);
        ValidateAllEnemyRuntime(map);
        ValidateFrontierEnemyPolish(map);
        ValidateCorruptionAndAnchors(map);
        ValidateRiftSafety(map);
        ValidateStatusBuffAndDisplacement(map);
        ValidateHellbruteSafety(map);
        ValidateBossFoundation(map);
        ValidateAdminTeleports();
        ValidateRestartCleanupAndCompletion();
    }

    private static void ValidateContinuousGround(DungeonMap map)
    {
        int previous = int.MaxValue;
        for (int x = map.WorldBounds.Left + 4;
             x < map.WorldBounds.Right - 4;
             x += 8)
        {
            int top = MandatoryTop(map, x);
            Need(top != int.MaxValue, $"Demon Lands main route gap near X={x}.");
            if (previous != int.MaxValue)
                Need(Math.Abs(top - previous) <= 32,
                    $"Demon Lands main route step is unsafe near X={x}.");
            previous = top;
        }
    }

    private static void ValidateActualPlayerTraversal(DungeonMap map)
    {
        Vector2 size = new(
            Player.Player.BodyHurtboxWidth,
            Player.Player.BodyHurtboxHeight);
        Vector2 start = SideScrollingCollision.ResolveSafeEntrySpawn(
            map.WorldBounds.Left + 105f,
            size,
            map,
            0f);
        var player = new Player.Player(start);
        player.UpdateSideScrollingMovement(Frame, new KeyboardState(), false, map);
        float target = map.ExitRoom.Bounds.Right - 160f;
        float priorX = player.Position.X;
        int stillFrames = 0;
        for (int frame = 0; frame < 7200 && player.Position.X < target; frame++)
        {
            bool jump = stillFrames >= 8 && player.IsGrounded;
            player.UpdateSideScrollingMovement(
                Frame,
                new KeyboardState(Keys.D),
                jump,
                map);
            if (player.Position.X <= priorX + .05f)
                stillFrames++;
            else
                stillFrames = 0;
            priorX = player.Position.X;
        }
        Need(player.Position.X >= target,
            "Actual Player A/D + Space simulation could not traverse Map 4 without dash or attacks.");
        Need(map.WorldBounds.Contains(player.Bounds),
            "Actual Player traversal left the Map 4 world bounds.");

        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Raised)
                continue;
            Need(platform.Bounds.Width >= Player.Player.BodyHurtboxWidth * 2f,
                "A Map 4 optional platform is narrower than the Player collider.");
            Vector2 landing = new(
                platform.Bounds.Center.X,
                platform.Bounds.Top - size.Y / 2f);
            Need(SideScrollingCollision.IsPositionFree(landing, size, map) &&
                SideScrollingCollision.IsSupported(landing, size, map),
                "A Map 4 optional platform lacks a safe landing.");
        }
    }

    private static void ValidateRoster(DungeonMap map)
    {
        var roster = new HashSet<EnemyType>();
        foreach (SpawnSocket socket in map.SpawnSockets)
            foreach (EnemyType type in socket.SuggestedEnemyTypes)
                roster.Add(type);
        EnemyType[] required =
        {
            EnemyType.ImpRaider, EnemyType.HellArcher,
            EnemyType.SpineHound, EnemyType.RiftStalker,
            EnemyType.BoneHarvester, EnemyType.Hellbrute,
            EnemyType.CorruptionPriest, EnemyType.RiftWarden,
            EnemyType.DemonLegionnaire, EnemyType.HornedGeneral
        };
        foreach (EnemyType type in required)
        {
            Need(roster.Contains(type), $"Map 4 omits {type}.");
            Enemy enemy = EnemyFactory.Create(
                type,
                new EnemySpawnContext(
                    Vector2.Zero,
                    5,
                    1,
                    0,
                    false,
                    GoblinVariant.Normal));
            Need(enemy is DemonLandsEnemy && enemy.MaxHealth > 0,
                $"{type} basic combat factory failed.");
        }
    }

    private static void ValidateAllEnemyRuntime(DungeonMap map)
    {
        EnemyType[] required =
        {
            EnemyType.ImpRaider, EnemyType.HellArcher,
            EnemyType.SpineHound, EnemyType.RiftStalker,
            EnemyType.BoneHarvester, EnemyType.Hellbrute,
            EnemyType.CorruptionPriest, EnemyType.RiftWarden,
            EnemyType.DemonLegionnaire, EnemyType.HornedGeneral
        };
        float[] testRanges = { 100f, 260f, 120f, 150f, 150f,
            220f, 250f, 180f, 120f, 170f };
        DungeonRoom arena = map.BossRoom;

        int socketIndex = 0;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            DungeonRoom socketRoom = map.FindRoomContaining(socket.Position);
            foreach (EnemyType socketType in socket.SuggestedEnemyTypes)
            {
                var socketManager = new EnemyManager(4300 + socketIndex++);
                Enemy socketEnemy = socketManager.SpawnAuthoredEnemy(
                    socketType, socket, socketRoom, map, 5, 1);
                Need(socketEnemy != null &&
                    SideScrollingCollision.IsPositionFree(
                        socketEnemy.Position, socketEnemy.Size, map) &&
                    (socketEnemy.IsFlying || SideScrollingCollision.IsSupported(
                        socketEnemy.Position, socketEnemy.Size, map)),
                    $"{socket.SocketId} cannot safely spawn {socketType}.");
            }
        }

        for (int index = 0; index < required.Length; index++)
        {
            EnemyType type = required[index];
            SpawnSocket socket = FindSocket(map, type);
            Need(socket != null, $"{type} has no authored spawn socket.");
            DungeonRoom socketRoom = map.FindRoomContaining(socket.Position);
            var spawnManager = new EnemyManager(4404 + index);
            Enemy spawned = spawnManager.SpawnAuthoredEnemy(
                type, socket, socketRoom, map, 5, 1);
            Need(spawned != null &&
                SideScrollingCollision.IsPositionFree(
                    spawned.Position, spawned.Size, map) &&
                SideScrollingCollision.IsSupported(
                    spawned.Position, spawned.Size, map),
                $"{type} authored spawn is not valid and grounded.");

            var enemy = new DemonLandsEnemy(
                type,
                new Vector2(arena.Bounds.Center.X - testRanges[index],
                    arena.GroundY - 100f),
                5,
                1,
                arena.Id,
                type is EnemyType.RiftWarden or
                    EnemyType.DemonLegionnaire or EnemyType.HornedGeneral);
            enemy.SnapToGround(arena);
            Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
                arena.Bounds.Center.X,
                new Vector2(Player.Player.BodyHurtboxWidth,
                    Player.Player.BodyHurtboxHeight),
                arena);
            var player = new Player.Player(playerPosition);
            var projectiles = new ProjectileManager();
            var manager = new EnemyManager(5500 + index);
            bool sawBehavior = false;
            bool sawTelegraph = false;
            bool sawAttack = false;
            bool sawFacingPlayer = false;
            Vector2 start = enemy.Position;
            for (int frame = 0; frame < 420; frame++)
            {
                enemy.Update(Frame, player, map, projectiles,
                    rootHazards: null, enemies: manager);
                projectiles.Update(Frame, player, map);
                sawBehavior |= enemy.FrontierCombatState !=
                    DemonFrontierCombatState.Ready ||
                    Vector2.DistanceSquared(start, enemy.Position) > 1f ||
                    projectiles.Projectiles.Count > 0;
                sawTelegraph |= enemy.IsCombatAttackTelegraphing;
                sawAttack |= enemy.IsCombatAttackActive;
                sawFacingPlayer |= enemy.Position.X <= player.Position.X
                    ? enemy.Facing == EnemyFacingDirection.Right
                    : enemy.Facing == EnemyFacingDirection.Left;
                Need(map.WorldBounds.Contains(enemy.Bounds),
                    $"{type} left the playable world during its combat loop.");
            }
            Need(sawBehavior && sawTelegraph && sawAttack,
                $"{type} did not complete an aggro/telegraph/attack cycle.");
            Need(sawFacingPlayer,
                $"{type} never faced toward the Player.");

            enemy.UpdateHealthBarPresentation(Frame, player.Position,
                isActive: true);
            Need(enemy.IsHealthBarVisible,
                $"{type} did not expose its combat HP bar.");

            var introductions = new EnemyIntroductionManager();
            introductions.Update(Frame, enemy.Position,
                new Enemy[] { enemy }, boss: null,
                allowBossIntroduction: false);
            Need(introductions.Active != null &&
                introductions.Active.Identity == IdentityFor(type),
                $"{type} first-encounter presentation is not registered.");
            string key = IntroductionKey(type);
            Need(HasLocalizedIntroduction(key, Language.English) &&
                HasLocalizedIntroduction(key, Language.Vietnamese),
                $"{type} is missing EN/VI first-encounter localization.");

            enemy.ApplyPoiseDamage(enemy.MaxPoise);
            Need(enemy.IsStaggered,
                $"{type} did not enter stagger after full Poise damage.");
            for (int frame = 0; frame < 240 && enemy.IsStaggered; frame++)
                enemy.UpdateTimers(Frame);
            Need(!enemy.IsStaggered,
                $"{type} remained permanently staggered.");

            enemy.ReceiveDamage(int.MaxValue);
            Need(!enemy.IsAlive && !enemy.CanBeTargeted &&
                !enemy.IsDeathPresentationComplete,
                $"{type} death state did not start correctly.");
            for (int frame = 0; frame < 240; frame++)
                enemy.UpdateDeathPresentation(Frame);
            Need(enemy.IsDeathPresentationComplete,
                $"{type} death presentation did not complete.");
        }
    }

    private static SpawnSocket FindSocket(DungeonMap map, EnemyType type)
    {
        foreach (SpawnSocket socket in map.SpawnSockets)
            foreach (EnemyType suggested in socket.SuggestedEnemyTypes)
                if (suggested == type)
                    return socket;
        return null;
    }

    private static WildForestEnemyIdentity IdentityFor(EnemyType type) =>
        type switch
        {
            EnemyType.ImpRaider => WildForestEnemyIdentity.ImpRaider,
            EnemyType.HellArcher => WildForestEnemyIdentity.HellArcher,
            EnemyType.SpineHound => WildForestEnemyIdentity.SpineHound,
            EnemyType.RiftStalker => WildForestEnemyIdentity.RiftStalker,
            EnemyType.BoneHarvester => WildForestEnemyIdentity.BoneHarvester,
            EnemyType.Hellbrute => WildForestEnemyIdentity.Hellbrute,
            EnemyType.CorruptionPriest =>
                WildForestEnemyIdentity.CorruptionPriest,
            EnemyType.RiftWarden => WildForestEnemyIdentity.RiftWarden,
            EnemyType.DemonLegionnaire =>
                WildForestEnemyIdentity.DemonLegionnaire,
            EnemyType.HornedGeneral => WildForestEnemyIdentity.HornedGeneral,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static string IntroductionKey(EnemyType type) => type switch
    {
        EnemyType.ImpRaider => "imp_raider",
        EnemyType.HellArcher => "hell_archer",
        EnemyType.SpineHound => "spine_hound",
        EnemyType.RiftStalker => "rift_stalker",
        EnemyType.BoneHarvester => "bone_harvester",
        EnemyType.Hellbrute => "hellbrute",
        EnemyType.CorruptionPriest => "corruption_priest",
        EnemyType.RiftWarden => "rift_warden",
        EnemyType.DemonLegionnaire => "demon_legionnaire",
        EnemyType.HornedGeneral => "horned_general",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static bool HasLocalizedIntroduction(string key, Language language)
    {
        IReadOnlyDictionary<string, string> strings = Loc.GetStrings(language);
        return strings.TryGetValue($"enemy.{key}.name", out string name) &&
            !string.IsNullOrWhiteSpace(name) &&
            strings.TryGetValue($"enemy.{key}.title", out string title) &&
            !string.IsNullOrWhiteSpace(title) &&
            strings.TryGetValue($"enemy.{key}.description", out string description) &&
            !string.IsNullOrWhiteSpace(description);
    }

    private static void ValidateCorruptionAndAnchors(DungeonMap map)
    {
        var corruption = new DemonicCorruptionSystem();
        var anchors = new CorruptionAnchorSystem();
        anchors.Reset(map);
        corruption.Add(24.99f);
        Need(corruption.State == DemonicCorruptionState.Stable,
            "Stable Corruption threshold failed.");
        corruption.Add(.01f);
        Need(corruption.State == DemonicCorruptionState.Tainted,
            "Tainted Corruption threshold failed.");
        corruption.Add(25f);
        Need(corruption.State == DemonicCorruptionState.Corrupted,
            "Corrupted Corruption threshold failed.");
        corruption.Add(25f);
        Need(corruption.State == DemonicCorruptionState.Overrun,
            "Overrun Corruption threshold failed.");
        corruption.Add(1000f);
        Need(Math.Abs(corruption.Value - DemonicCorruptionSystem.Maximum) < .001f,
            "Corruption exceeded its maximum bound.");
        corruption.Reduce(1000f);
        Need(corruption.Value == 0f &&
            corruption.State == DemonicCorruptionState.Stable,
            "Corruption did not clear cleanly.");

        foreach (CorruptionFieldDefinition field in map.CorruptionFields)
        {
            int ground = MandatoryTop(map, field.Bounds.Center.X);
            var groundedCenter = new Point(
                field.Bounds.Center.X,
                ground - (int)Player.Player.BodyHurtboxHeight / 2);
            Need(field.Bounds.Contains(groundedCenter),
                $"Corruption field {field.FieldId} misses the grounded Player center.");
        }

        CorruptionFieldDefinition freeField = null;
        foreach (CorruptionFieldDefinition field in map.CorruptionFields)
            if (field.AnchorId.Length == 0)
            {
                freeField = field;
                break;
            }
        Need(freeField != null,
            "Map 4 requires an unanchored field for Corruption validation.");
        int freeGround = MandatoryTop(map, freeField.Bounds.Center.X);
        var fieldPlayer = new Player.Player(new Vector2(
            freeField.Bounds.Center.X,
            freeGround - Player.Player.BodyHurtboxHeight / 2f));
        corruption.Update(Frame, fieldPlayer, map, anchors);
        Need(corruption.Value > 0f && corruption.Value <=
            freeField.GainPerSecond / 60f + .01f,
            "Standing on corrupted ground did not add bounded Corruption.");

        corruption.Add(60f);
        PurificationShrineDefinition shrine = map.PurificationShrines[0];
        var shrinePlayer = new Player.Player(new Vector2(
            shrine.Position.X,
            shrine.Position.Y - Player.Player.BodyHurtboxHeight / 2f));
        float beforePurification = corruption.Value;
        corruption.Update(Frame, shrinePlayer, map, anchors);
        Need(corruption.Value < beforePurification,
            "Purification shrine did not reduce Corruption.");

        CorruptionAnchorDefinition first = map.CorruptionAnchors[0];
        Rectangle hit = new(
            (int)first.Position.X - 30,
            (int)first.Position.Y - 90,
            60,
            90);
        Need(anchors.TryDamage(hit, 1, 90, corruption),
            "Anchor did not accept a player hit.");
        Need(anchors.TryDamage(hit, 2, 1000, corruption),
            "Anchor did not accept its destruction hit.");
        Need(!anchors.IsActive(first.AnchorId),
            "Anchor remained active after destruction.");
        foreach (CorruptionAnchorDefinition anchor in map.CorruptionAnchors)
        {
            Need(map.WorldBounds.Contains(anchor.Position.ToPoint()) &&
                MandatoryTop(map, (int)anchor.Position.X) != int.MaxValue,
                $"Anchor {anchor.AnchorId} is outside valid optional geometry.");
        }
        anchors.Reset(map);
        Need(anchors.IsActive(first.AnchorId),
            "Anchor reset did not restore the encounter.");
    }

    private static void ValidateRiftSafety(DungeonMap map)
    {
        DemonLandsZoneDefinition riftZone = map.DemonLandsZones[3];
        int[] playerXs =
        {
            riftZone.Bounds.Left + 84,
            riftZone.Bounds.Left + 310,
            riftZone.Bounds.Left + 610,
            riftZone.Bounds.Right - 92
        };
        foreach (int playerX in playerXs)
        {
            StressRiftEnemy(map, EnemyType.RiftStalker, playerX, 120f);
            StressRiftEnemy(map, EnemyType.RiftWarden, playerX, 42f);
        }

        DungeonRoom arena = map.BossRoom;
        var general = new DemonLandsEnemy(
            EnemyType.HornedGeneral,
            new Vector2(arena.Bounds.Left + 135f, arena.GroundY - 60f),
            5, 1, arena.Id, true);
        general.SnapToGround(arena);
        var player = new Player.Player(SideScrollingCollision.PlaceOnGround(
            general.Position.X + 175f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena));
        var projectiles = new ProjectileManager();
        general.ReceiveDamage((int)(general.MaxHealth * .42f));
        bool sawRiftPath = false;
        for (int frame = 0; frame < 360; frame++)
        {
            general.Update(Frame, player, map, projectiles);
            if (general.HasGeneralRiftPath)
            {
                sawRiftPath = true;
                Need(general.GeneralRiftPathBounds.Intersects(map.WorldBounds),
                    "Horned General created a rift path outside playable space.");
            }
            Need(map.WorldBounds.Contains(general.Bounds) &&
                SideScrollingCollision.IsPositionFree(
                    general.Position, general.Size, map),
                "Horned General rift behavior clipped world geometry.");
        }
        Need(sawRiftPath,
            "Horned General did not expose a bounded rift path in phase 2.");
    }

    private static void StressRiftEnemy(
        DungeonMap map,
        EnemyType type,
        int playerX,
        float separation)
    {
        DemonLandsZoneDefinition zone = map.DemonLandsZones[3];
        Vector2 playerPosition = SideScrollingCollision.ResolveSafeEntrySpawn(
            playerX,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            map,
            0f);
        var player = new Player.Player(playerPosition);
        float minimumEnemyX = zone.Bounds.Left + 42f;
        float maximumEnemyX = zone.Bounds.Right - 42f;
        float enemyX = playerPosition.X + separation <= maximumEnemyX
            ? playerPosition.X + separation
            : playerPosition.X - separation;
        enemyX = Math.Clamp(enemyX, minimumEnemyX, maximumEnemyX);
        var enemy = new DemonLandsEnemy(type,
            new Vector2(enemyX, zone.GroundY - 80f),
            5, 1, 3, type == EnemyType.RiftWarden);
        enemy.SnapToGround(map.Rooms[3]);
        var projectiles = new ProjectileManager();
        bool enteredHidden = false;
        bool exitedRift = false;
        for (int frame = 0; frame < 210; frame++)
        {
            enemy.Update(Frame, player, map, projectiles);
            bool hidden = enemy.IsRiftHidden || enemy.IsWardenHidden;
            enteredHidden |= hidden;
            Need(!hidden || !enemy.IsCombatAttackActive,
                $"{type} produced an invisible active attack while hidden.");
            if (enemy.HasRiftExitPosition)
            {
                exitedRift = true;
                Rectangle destinationBounds = DungeonCollision.CreateBounds(
                    enemy.RiftExitPosition, enemy.Size);
                Rectangle playerExclusion = player.Bounds;
                playerExclusion.Inflate(18, 10);
                Need(map.WorldBounds.Contains(destinationBounds) &&
                    SideScrollingCollision.IsPositionFree(
                        enemy.RiftExitPosition, enemy.Size, map) &&
                    SideScrollingCollision.IsSupported(
                        enemy.RiftExitPosition, enemy.Size, map) &&
                    !destinationBounds.Intersects(playerExclusion),
                    $"{type} selected an unsafe or overlapping rift exit.");
            }
            Need(map.WorldBounds.Contains(enemy.Bounds),
                $"{type} left playable space during rift movement.");
        }
        Need(enteredHidden,
            $"{type} did not enter its rift movement state during stress testing.");
        Need(exitedRift || enemy.FrontierCombatState ==
                DemonFrontierCombatState.Ready || enemy.IsCombatAttackTelegraphing,
            $"{type} became stuck after a failed rift destination.");
        Need(!enemy.IsRiftHidden && !enemy.IsWardenHidden,
            $"{type} remained hidden after its rift sequence.");
    }

    private static void ValidateStatusBuffAndDisplacement(DungeonMap map)
    {
        var corruption = new DemonicCorruptionSystem();
        var anchors = new CorruptionAnchorSystem();
        anchors.Reset(map);
        CorruptionAnchorDefinition definition = map.CorruptionAnchors[0];
        DungeonRoom room = map.FindRoomContaining(definition.Position);
        var priest = new DemonLandsEnemy(
            EnemyType.CorruptionPriest,
            definition.Position + new Vector2(-65f, -40f),
            5, 1, room.Id, true);
        priest.SnapToGround(room);
        Need(anchors.TryBlessNearestActive(priest, priest.Position, 370f,
                out string blessedId, out Vector2 blessedPosition) &&
            anchors.IsBlessed(blessedId),
            "Corruption Priest could not apply its bounded Anchor blessing.");
        priest.ConfirmAnchorBlessing(blessedId, blessedPosition);
        Need(priest.HasAnchorTether,
            "Priest blessing did not create its visible bounded tether state.");
        priest.ReceiveDamage(int.MaxValue);
        anchors.Update(Frame, map,
            new Player.Player(SideScrollingCollision.PlaceOnGround(
                definition.Position.X - 120f,
                new Vector2(Player.Player.BodyHurtboxWidth,
                    Player.Player.BodyHurtboxHeight), room)),
            new EnemyManager(991), corruption, 5, 1);
        Need(!anchors.IsBlessed(blessedId),
            "Anchor blessing survived its Priest source death.");

        anchors.Reset(map);
        var buffManager = new EnemyManager(992);
        SpawnSocket nearbySocket = null;
        float nearestDistanceSquared = float.MaxValue;
        foreach (SpawnSocket candidate in map.SpawnSockets)
        {
            bool supportsImp = false;
            foreach (EnemyType suggested in candidate.SuggestedEnemyTypes)
                supportsImp |= suggested == EnemyType.ImpRaider;
            float distanceSquared = Vector2.DistanceSquared(
                candidate.Position, definition.Position);
            if (supportsImp && distanceSquared < nearestDistanceSquared)
            {
                nearbySocket = candidate;
                nearestDistanceSquared = distanceSquared;
            }
        }
        Need(nearbySocket != null && nearestDistanceSquared <=
            definition.InfluenceRadius * definition.InfluenceRadius,
            "No Imp Raider socket exists inside the Anchor influence radius.");
        Enemy nearby = buffManager.SpawnAuthoredEnemy(
            EnemyType.ImpRaider,
            nearbySocket,
            map.FindRoomContaining(nearbySocket.Position),
            map, 5, 1);
        anchors.Update(Frame, map,
            new Player.Player(SideScrollingCollision.PlaceOnGround(
                definition.Position.X - 200f,
                new Vector2(Player.Player.BodyHurtboxWidth,
                    Player.Player.BodyHurtboxHeight), room)),
            buffManager, corruption, 5, 1);
        Need(nearby.IsBuffed,
            "Active Anchor did not apply its local enemy buff.");
        Rectangle anchorHit = new((int)definition.Position.X - 30,
            (int)definition.Position.Y - 90, 60, 90);
        Need(anchors.TryDamage(anchorHit, 41, int.MaxValue, corruption),
            "Anchor destruction fixture failed.");
        for (int frame = 0; frame < 30; frame++)
        {
            nearby.UpdateTimers(Frame);
            anchors.Update(Frame, map,
                new Player.Player(Vector2.Zero), buffManager,
                corruption, 5, 1);
        }
        Need(!nearby.IsBuffed,
            "Destroyed Anchor continued refreshing its enemy buff.");

        anchors.Reset(map);
        DungeonRoom arena = map.BossRoom;
        var boss = new DemonLandsEnemy(EnemyType.HornedGeneral,
            SideScrollingCollision.PlaceOnGround(arena.Bounds.Center.X,
                new Vector2(78f, 102f), arena),
            5, 1, arena.Id, true);
        var bossPlayer = new Player.Player(
            SideScrollingCollision.PlaceOnGround(arena.Bounds.Center.X - 260f,
                new Vector2(Player.Player.BodyHurtboxWidth,
                    Player.Player.BodyHurtboxHeight), arena));
        for (int attempt = 0; attempt < 5; attempt++)
            anchors.TrySummonBossAnchor(boss,
                boss.Position + new Vector2(-280f + attempt * 140f, 0f),
                map, bossPlayer, out _);
        Need(anchors.CountActiveTemporaryBossAnchors(boss) ==
            CorruptionAnchorSystem.MaximumTemporaryBossAnchors,
            "Horned General exceeded or failed its temporary Anchor cap.");

        var pulled = new Player.Player(SideScrollingCollision.PlaceOnGround(
            map.WorldBounds.Left + 34f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight), map.StartRoom));
        Vector2 beforePull = pulled.Position;
        pulled.PullToward(new Vector2(map.WorldBounds.Left - 500f,
            pulled.Position.Y), DemonLandsEnemy.HarvesterMaximumPullDistance, map);
        Need(Vector2.Distance(beforePull, pulled.Position) <=
                DemonLandsEnemy.HarvesterMaximumPullDistance + .01f &&
            map.WorldBounds.Contains(pulled.Bounds) &&
            SideScrollingCollision.IsPositionFree(
                pulled.Position, pulled.Size, map),
            "Harvester Hook pull crossed collision or exceeded its cap.");

        ValidateEmberProjectileCleanup(map);
    }

    private static void ValidateEmberProjectileCleanup(DungeonMap map)
    {
        DungeonRoom room = map.Rooms[0];
        Platform obstruction = null;
        foreach (Platform platform in room.Platforms)
            if (platform.Kind == PlatformKind.Raised)
            {
                obstruction = platform;
                break;
            }
        Need(obstruction != null,
            "Hell Archer terrain-collision fixture has no obstruction.");
        var projectiles = new ProjectileManager();
        Vector2 source = new(obstruction.Bounds.Left - 80f,
            obstruction.Bounds.Center.Y);
        Vector2 target = new(obstruction.Bounds.Right + 100f,
            obstruction.Bounds.Center.Y);
        projectiles.SpawnHellArrow(source, target, 12, room.Id,
            highArc: false);
        var player = new Player.Player(SideScrollingCollision.PlaceOnGround(
            room.Bounds.Right - 90f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight), room));
        for (int frame = 0; frame < 240 &&
            projectiles.Projectiles.Count > 0; frame++)
            projectiles.Update(Frame, player, map);
        Need(projectiles.Projectiles.Count == 0,
            "Ember Arrow survived terrain impact or its bounded lifetime.");
        Need(!player.IsSlowed,
            "Ember Arrow leaked an unrelated persistent Player status.");
    }

    private static void ValidateHellbruteSafety(DungeonMap map)
    {
        DungeonRoom arena = map.BossRoom;
        Vector2 bruteSize = new Vector2(82f, 94f) * 1.15f;
        var brute = new DemonLandsEnemy(
            EnemyType.Hellbrute,
            SideScrollingCollision.ResolveSafeEntrySpawn(
                arena.Bounds.Right - 650f, bruteSize, map, 0f),
            5, 1, arena.Id, true);
        var player = new Player.Player(SideScrollingCollision.ResolveSafeEntrySpawn(
            arena.Bounds.Right - 470f,
            new Vector2(Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight), map, 0f));
        var projectiles = new ProjectileManager();
        bool sawCharge = false;
        for (int frame = 0; frame < 240; frame++)
        {
            brute.Update(Frame, player, map, projectiles);
            sawCharge |= brute.HellbruteAttack ==
                HellbruteAttackKind.InfernalCharge;
            Need(map.WorldBounds.Contains(brute.Bounds) &&
                SideScrollingCollision.IsPositionFree(
                    brute.Position, brute.Size, map),
                "Hellbrute Charge left playable terrain or clipped geometry.");
            if (brute.IsCombatAttackActive)
                Need(brute.ActiveAttackArea.Intersects(map.WorldBounds),
                    "Hellbrute produced an attack hitbox outside the world.");
        }
        Need(sawCharge,
            "Hellbrute did not exercise Infernal Charge in its safety test.");

        brute.ReceiveDamage((int)(brute.MaxHealth * .75f));
        int enrageEntries = 0;
        DemonFrontierCombatState previous = brute.FrontierCombatState;
        for (int frame = 0; frame < 360; frame++)
        {
            brute.Update(Frame, player, map, projectiles);
            if (brute.FrontierCombatState == DemonFrontierCombatState.BruteEnrage &&
                previous != DemonFrontierCombatState.BruteEnrage)
                enrageEntries++;
            previous = brute.FrontierCombatState;
        }
        Need(brute.IsEnragedBrute && enrageEntries == 1,
            "Hellbrute low-health enrage was missing or repeated.");
        Need(map.CollapsingStructures.Count == 0,
            "Demon Lands unexpectedly exposes traversal-breaking destructibles.");
    }

    private static void ValidateFrontierEnemyPolish(DungeonMap map)
    {
        DungeonRoom arena = map.BossRoom;
        Vector2 impPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X - 140f,
            new Vector2(38f, 48f),
            arena);
        Vector2 playerPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(
                Player.Player.BodyHurtboxWidth,
                Player.Player.BodyHurtboxHeight),
            arena);
        var player = new Player.Player(playerPosition);
        var projectiles = new ProjectileManager();
        var imp = new DemonLandsEnemy(
            EnemyType.ImpRaider,
            impPosition,
            1,
            1,
            arena.Id,
            false);

        Need(imp.MaxHealth < 100 && imp.MaxPoise <= 32f,
            "Imp Raider is not configured as a low-health, low-Poise swarm enemy.");
        imp.Update(Frame, player, map, projectiles);
        Need(imp.ImpRaiderAttack == ImpRaiderAttackKind.PouncingCut &&
            imp.FrontierCombatState == DemonFrontierCombatState.ImpPounceCrouch,
            "Imp Raider pounce lacks its readable crouch state.");

        var desperateImp = new DemonLandsEnemy(
            EnemyType.ImpRaider,
            playerPosition + new Vector2(-48f, 0f),
            1,
            1,
            arena.Id,
            false);
        desperateImp.ReceiveDamage((int)(desperateImp.MaxHealth * .70f));
        for (int index = 0; index < 12 &&
             desperateImp.ImpRaiderAttack == ImpRaiderAttackKind.None;
             index++)
        {
            desperateImp.Update(Frame, player, map, projectiles);
        }
        Need(desperateImp.ImpRaiderAttack == ImpRaiderAttackKind.FrenziedStab &&
            DemonLandsEnemy.ImpMaximumFrenzyStabs == 3,
            $"Imp Raider desperation sequence is missing or unbounded " +
            $"(attack={desperateImp.ImpRaiderAttack}, " +
            $"health={desperateImp.CurrentHealth}/{desperateImp.MaxHealth}, " +
            $"distance={Vector2.Distance(desperateImp.Position, player.Position):0.0}).");

        var archer = new DemonLandsEnemy(
            EnemyType.HellArcher,
            impPosition - new Vector2(190f, 0f),
            1,
            1,
            arena.Id,
            false);
        archer.Update(Frame, player, map, projectiles);
        Need(archer.FrontierCombatState == DemonFrontierCombatState.ArcherDraw &&
            projectiles.Projectiles.Count == 0,
            "Hell Archer must visibly draw before spawning a projectile.");
        archer.ApplyPoiseDamage(archer.MaxPoise);
        for (int index = 0; index < 10; index++)
            archer.Update(Frame, player, map, projectiles);
        Need(projectiles.Projectiles.Count == 0,
            "An interrupted Hell Archer draw fired an invisible projectile.");

        projectiles.SpawnHellArrow(
            impPosition,
            playerPosition,
            14,
            arena.Id,
            highArc: false);
        projectiles.SpawnHellArrow(
            impPosition,
            playerPosition,
            15,
            arena.Id,
            highArc: true);
        Need(projectiles.Projectiles.Count == 2 &&
            projectiles.Projectiles[0].IsEmberArrow &&
            projectiles.Projectiles[0].Trajectory ==
                ProjectileTrajectoryType.Straight &&
            projectiles.Projectiles[1].IsEmberArrow &&
            projectiles.Projectiles[1].Trajectory ==
                ProjectileTrajectoryType.Ballistic &&
            !projectiles.Projectiles[1].PenetratesTerrain,
            "Hell Archer straight/high-arc projectile contract failed.");

        var introductions = new EnemyIntroductionManager();
        introductions.Update(
            Frame,
            imp.Position,
            new List<Enemy> { imp },
            boss: null,
            allowBossIntroduction: false);
        Need(introductions.Active?.Identity ==
                WildForestEnemyIdentity.ImpRaider,
            "Imp Raider first-encounter presentation is not registered.");
    }

    private static void ValidateBossFoundation(DungeonMap map)
    {
        DungeonRoom arena = map.BossRoom;
        Vector2 bossPosition = SideScrollingCollision.PlaceOnGround(
            arena.Bounds.Center.X,
            new Vector2(78f, 102f),
            arena);
        var boss = new DemonLandsEnemy(
            EnemyType.HornedGeneral,
            bossPosition,
            5,
            1,
            arena.Id,
            true);
        var player = new Player.Player(bossPosition + new Vector2(120f, 0f));
        var projectiles = new ProjectileManager();

        boss.ReceiveDamage((int)(boss.MaxHealth * .42f));
        boss.Update(Frame, player, map, projectiles);
        Need(boss.CombatPhase == 2 &&
            boss.FrontierCombatState ==
                DemonFrontierCombatState.GeneralPhaseTransition &&
            !boss.IsCombatAttackActive,
            "Horned General phase 2 transition is not a safe dramatic pause.");
        for (int frame = 0; frame < 90 &&
             boss.FrontierCombatState != DemonFrontierCombatState.Ready;
             frame++)
        {
            boss.UpdateTimers(Frame);
            boss.Update(Frame, player, map, projectiles);
        }

        var anchors = new CorruptionAnchorSystem();
        anchors.Reset(map);
        Need(anchors.TrySummonBossAnchor(
                boss,
                player.Position + new Vector2(180f, 0f),
                map,
                player,
                out string summonedAnchorId) &&
            anchors.IsActive(summonedAnchorId) &&
            anchors.CountActiveTemporaryBossAnchors(boss) == 1,
            "Horned General temporary Anchor summon failed validation.");
        boss.ReceiveDamage((int)(boss.MaxHealth * .36f));
        boss.Update(Frame, player, map, projectiles);
        Need(boss.CombatPhase == 3 && boss.IsLegionsEndTelegraphing,
            "Horned General did not enter phase 3 with Legion's End.");
        bool sawLegionsEndCharge = false;
        bool sawLegionsEndCleave = false;
        bool sawLegionsEndRecovery = false;
        for (int frame = 0; frame < 300 && !sawLegionsEndRecovery; frame++)
        {
            boss.UpdateTimers(Frame);
            boss.Update(Frame, player, map, projectiles);
            sawLegionsEndCharge |= boss.FrontierCombatState ==
                DemonFrontierCombatState.GeneralLegionsEndCharge;
            sawLegionsEndCleave |= boss.FrontierCombatState ==
                DemonFrontierCombatState.GeneralLegionsEndCleave;
            sawLegionsEndRecovery |= boss.FrontierCombatState ==
                DemonFrontierCombatState.GeneralLegionsEndRecovery;
            Need(map.WorldBounds.Contains(boss.Bounds),
                "Legion's End moved the Horned General outside playable bounds.");
        }
        Need(sawLegionsEndCharge && sawLegionsEndCleave &&
            sawLegionsEndRecovery,
            "Legion's End did not complete its telegraph, charge, cleave, and recovery sequence.");

        var legionnaire = new DemonLandsEnemy(
            EnemyType.DemonLegionnaire,
            bossPosition - new Vector2(132f, 0f),
            5,
            1,
            arena.Id,
            true);
        var legionPlayer = new Player.Player(
            legionnaire.Position + new Vector2(120f, 0f));
        legionnaire.Update(Frame, legionPlayer, map, projectiles);
        Need(legionnaire.IsLegionGuarding &&
            legionnaire.DemonLegionnaireAttack ==
                DemonLegionnaireAttackKind.LegionThrust,
            "Demon Legionnaire does not enter shield-first formation pressure.");
        legionnaire.ReceiveDamageFrom(
            46,
            52f,
            legionPlayer.Position);
        Need(legionnaire.IsGuardBroken,
            "Demon Legionnaire guard did not break under heavy/Poise pressure.");
    }

    private static void ValidateAdminTeleports()
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 4404);
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 4, 0);
        session.ExecuteDeveloperCommand(DeveloperCommand.ToggleGodMode, 4, 0);
        SettleEntry(session);
        Need(session.CurrentDungeon.IsDemonLands && session.DungeonDepth == 4,
            "Admin Map 4 teleport registration failed.");
        for (int zone = 0; zone < DemonLandsMapGenerator.ZoneCount; zone++)
        {
            session.ExecuteDeveloperCommand(DeveloperCommand.TeleportZone, 4, zone);
            SettleEntry(session);
            Need(session.GetCurrentZoneIndex() == zone,
                $"Admin Map 4 zone {zone + 1} teleport selected the wrong zone.");
            Need(SideScrollingCollision.IsPositionFree(
                    session.Player.Position,
                    session.Player.Size,
                    session.CurrentDungeon) &&
                SideScrollingCollision.IsSupported(
                    session.Player.Position,
                    session.Player.Size,
                    session.CurrentDungeon),
                $"Admin Map 4 zone {zone + 1} teleport is not safely grounded.");
        }
        session.ExecuteDeveloperCommand(
            DeveloperCommand.ClearCorruption,
            4,
            0);
        session.ExecuteDeveloperCommand(
            DeveloperCommand.ResetCorruptionAnchors,
            4,
            0);

        EncounterZone bossZone = session.CurrentDungeon.EncounterZones[^1];
        session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
            bossZone.ActivationBounds.Left + 3f,
            session.Player.Size,
            session.CurrentDungeon,
            dropOffset: 0f));
        for (int frame = 0; frame < 120; frame++)
            UpdateSession(session);
        DemonLandsEnemy originalGeneral = FindLivingDemon(
            session.Enemies.Enemies, EnemyType.HornedGeneral);
        Need(originalGeneral != null,
            "Admin Infernal Heart teleport did not activate the Horned General.");
        originalGeneral.ReceiveDamage((int)(originalGeneral.MaxHealth * .42f));
        UpdateSession(session);
        Need(session.CorruptionAnchors.TrySummonBossAnchor(
                originalGeneral,
                originalGeneral.Position + new Vector2(-220f, 0f),
                session.CurrentDungeon,
                session.Player,
                out _),
            "Admin boss-reset fixture could not create a temporary Anchor.");
        session.Projectiles.SpawnHellArrow(
            originalGeneral.Position,
            session.Player.Position,
            10,
            session.CurrentDungeon.BossRoom.Id,
            highArc: false);
        session.ExecuteDeveloperCommand(
            DeveloperCommand.ResetCurrentBoss,
            4,
            DemonLandsMapGenerator.ZoneCount - 1);
        Need(FindLivingDemon(session.Enemies.Enemies,
                EnemyType.HornedGeneral) == null &&
            session.CorruptionAnchors.CountActiveTemporaryBossAnchors() == 0 &&
            session.Projectiles.Projectiles.Count == 0 &&
            !session.Map04Cleared && !session.BossDefeated,
            "Admin Horned General reset retained boss-local state.");
        for (int frame = 0; frame < 120; frame++)
            UpdateSession(session);
        DemonLandsEnemy resetGeneral = FindLivingDemon(
            session.Enemies.Enemies, EnemyType.HornedGeneral);
        Need(resetGeneral != null && !ReferenceEquals(
                resetGeneral, originalGeneral),
            "Admin Horned General reset did not produce a clean encounter.");
    }

    private static void ValidateRestartCleanupAndCompletion()
    {
        string directory = Path.Combine(Path.GetTempPath(),
            "DungeonAscendant-map4-regression-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");
        try
        {
            var saveManager = new SaveSlotManager(path);
            var fixture = new SaveSlotData
            {
                SlotIndex = 0,
                Occupied = true,
                CharacterName = "MAP4 TEST",
                Appearance =
                    DungeonAscendant.Player.CharacterAppearance.CreateDefault(),
                Level = 5,
                DungeonDepth = 4,
                CurrentMapId = "map-04",
                CurrentArea = "BLACKENED FRONTIER",
                Map01Cleared = true,
                Map02Cleared = true,
                Map03Cleared = true,
                Map04Cleared = false,
                BossDefeated = false,
                LastPlayedUtc = DateTimeOffset.UtcNow
            };
            Need(saveManager.Store(fixture),
                "Map 4 completion fixture could not create a save.");

            var session = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 4404,
                savePath: path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.DungeonDepth == 4 &&
                session.CurrentDungeon.IsDemonLands,
                "Map 3 completion hook did not load the Map 4 milestone fixture.");

            for (int frame = 0; frame < 120; frame++)
                UpdateSession(session);
            session.Corruption.Add(72f);
            session.Projectiles.SpawnHellArrow(
                session.Player.Position + new Vector2(-100f, -20f),
                session.Player.Position + new Vector2(100f, -20f),
                10, session.CurrentDungeon.StartRoom.Id, highArc: false);
            session.Player.ApplySlow(.5f, 3f);
            session.Player.DebugGodMode = false;
            session.Player.ReceiveDamage(int.MaxValue);
            UpdateSession(session);
            Need(session.State == GameState.GameOver,
                "Map 4 Player death did not enter the restart state.");
            Press(session, Keys.R);
            Need(session.State == GameState.Playing &&
                session.DungeonDepth == 1 &&
                !session.CurrentDungeon.IsDemonLands &&
                session.Corruption.Value == 0f &&
                session.CorruptionAnchors.States.Count == 0 &&
                session.Projectiles.Projectiles.Count == 0 &&
                !session.Player.IsSlowed,
                "Death restart retained Map 4 transient state.");

            Need(new SaveSlotManager(path).Store(fixture),
                "Map 4 fixture could not be restored after death restart.");
            session = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 4404,
                savePath: path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.DungeonDepth == 4 &&
                session.CurrentDungeon.IsDemonLands,
                "Map 4 fixture did not reload after death restart validation.");

            session.Corruption.Add(84f);
            foreach (CorruptionAnchorRuntime anchor in
                session.CorruptionAnchors.States.Values)
            {
                anchor.BlessingTimeRemaining = 4f;
                break;
            }
            session.Projectiles.SpawnHellArrow(
                session.Player.Position + new Vector2(-100f, -20f),
                session.Player.Position + new Vector2(100f, -20f),
                10, session.CurrentDungeon.StartRoom.Id, highArc: false);
            session.Player.ApplySlow(.5f, 3f);
            Need(session.Corruption.Value > 0f &&
                session.CorruptionAnchors.States.Count > 0 &&
                session.Projectiles.Projectiles.Count > 0 &&
                session.Player.IsSlowed,
                "Map 4 cleanup fixture did not establish transient state.");

            ReturnThroughPauseMenu(session);
            Need(session.State == GameState.CharacterSelect &&
                session.CurrentDungeon == null &&
                session.Corruption.Value == 0f &&
                session.CorruptionAnchors.States.Count == 0 &&
                session.Projectiles.Projectiles.Count == 0 &&
                !session.Player.IsSlowed,
                "Character Select retained Map 4 Corruption, Anchors, projectile, or status state.");

            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.CurrentDungeon.IsDemonLands &&
                session.Corruption.Value == 0f &&
                session.Projectiles.Projectiles.Count == 0 &&
                !session.Player.IsSlowed,
                "Reloading Map 4 retained transient state.");

            EncounterZone bossZone = session.CurrentDungeon.EncounterZones[^1];
            session.Player.MoveTo(SideScrollingCollision.ResolveSafeEntrySpawn(
                bossZone.ActivationBounds.Left + 3f,
                session.Player.Size,
                session.CurrentDungeon,
                dropOffset: 0f));
            for (int frame = 0; frame < 100; frame++)
                UpdateSession(session);

            DemonLandsEnemy general = FindLivingDemon(
                session.Enemies.Enemies, EnemyType.HornedGeneral);
            Need(general != null,
                "Horned General did not spawn in the Infernal Heart encounter.");
            Need(session.CorruptionAnchors.TrySummonBossAnchor(
                    general,
                    general.Position + new Vector2(-220f, 0f),
                    session.CurrentDungeon,
                    session.Player,
                    out _),
                "Boss cleanup fixture could not create a temporary Anchor.");
            int killsBefore = session.KillCount;
            general.ReceiveDamage(int.MaxValue);
            for (int frame = 0; frame < 220; frame++)
                UpdateSession(session);

            Need(session.Map04Cleared && session.BossDefeated &&
                session.IsExitUnlocked,
                "Horned General defeat did not complete Map 4.");
            Need(session.KillCount == killsBefore + 1,
                "Horned General completion/reward was processed more than once.");
            Need(session.Corruption.Value == 0f &&
                session.CorruptionAnchors.CountActiveTemporaryBossAnchors() == 0 &&
                !session.CorruptionAnchors.IsActive("heart-anchor"),
                "Horned General death did not clean Corruption and boss Anchors.");
            Need(session.Enemies.DefeatPresentations.Count == 0,
                "Horned General death presentation did not complete.");

            session.Player.MoveTo(session.ExitPosition);
            Press(session, Keys.E);
            Need(session.Map04ExitPrepared && session.DungeonDepth == 4,
                "Completed Map 4 did not activate the future Map 5 hook.");
            session.FlushActiveSave();
            SaveSlotData completed = new SaveSlotManager(path).GetSlot(0);
            Need(completed.Map04Cleared && completed.BossDefeated &&
                completed.DungeonDepth == 4,
                "Map 4 completion did not persist to the save slot.");

            int completedKillCount = session.KillCount;
            ReturnThroughPauseMenu(session);
            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.Map04Cleared && session.BossDefeated &&
                session.WildForestEncounters.IsCleared("horned-general"),
                "Completed Map 4 state did not restore from save.");
            for (int frame = 0; frame < 120; frame++)
                UpdateSession(session);
            Need(FindLivingDemon(session.Enemies.Enemies,
                    EnemyType.HornedGeneral) == null &&
                session.KillCount == completedKillCount,
                "Completed save respawned or rewarded the Horned General again.");

            ReturnThroughPauseMenu(session);
            Need(session.State == GameState.CharacterSelect &&
                session.CurrentDungeon == null,
                "Character Select failed after Map 4 completion.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static DemonLandsEnemy FindLivingDemon(
        IReadOnlyList<Enemy> enemies,
        EnemyType type)
    {
        foreach (Enemy enemy in enemies)
            if (enemy is DemonLandsEnemy demon &&
                demon.Type == type && demon.IsAlive)
                return demon;
        return null;
    }

    private static void ReturnThroughPauseMenu(GameSession session)
    {
        Press(session, Keys.Escape);
        Press(session, Keys.S);
        Press(session, Keys.S);
        Press(session, Keys.Enter);
        Press(session, Keys.D);
        Press(session, Keys.Enter);
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame, new KeyboardState(key), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
    }

    private static void UpdateSession(GameSession session) => session.Update(
        Frame,
        new KeyboardState(),
        new MouseState());

    private static void SettleEntry(GameSession session)
    {
        for (int frame = 0; frame < 180 && !session.Player.IsGrounded; frame++)
        {
            session.Update(
                Frame,
                new KeyboardState(),
                new MouseState());
        }
    }

    private static int MandatoryTop(DungeonMap map, int x)
    {
        int top = int.MaxValue;
        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Ground ||
                x < platform.Bounds.Left || x >= platform.Bounds.Right)
                continue;
            top = Math.Min(top, platform.Bounds.Top);
        }
        return top;
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
