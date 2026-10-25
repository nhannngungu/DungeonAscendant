using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonAscendant.Audio;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Dungeon;

public static class DemonKingSanctumMapValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonKingSanctumMapGenerator().Generate();
        Need(map.IsDemonKingSanctum &&
            map.DemonKingSanctumZones.Count ==
                DemonKingSanctumMapGenerator.ZoneCount,
            "Map 7 did not register its ten authored sectors.");
        Need(map.WorldBounds.Width == DemonKingSanctumMapGenerator.WorldWidth &&
            map.WorldBounds.Width > DemonCityMapGenerator.WorldWidth &&
            map.WorldBounds.Width < 20000,
            "Map 7 length is not substantial and bounded.");
        Need(RegionDefinition.Get(RegionType.DemonKingSanctuary).IsImplemented,
            "Map 7 region registration is not active.");
        Need(map.StartRoom != null && map.TreasureRoom != null &&
            map.BossRoom != null && map.ExitRoom != null &&
            map.BossRoom != map.ExitRoom,
            "Map 7 room roles are incomplete.");
        Need(map.SanctumProps.Count >= 40 &&
            map.SanctumCheckpoints.Count == 5 &&
            map.EncounterZones.Count == 9 &&
            map.SpawnSockets.Count ==
                DemonKingSanctumMapGenerator.TotalSocketCount,
            "Map 7 foundation metadata is incomplete.");

        ValidateConnections(map);
        ValidateContinuousRoute(map);
        ValidatePlayerTraversal(map);
        ValidateCheckpoints(map);
        ValidateSocketsAndEnemies(map);
        ValidateBossArenaAndEndpoint(map);
        ValidateResources(map);
        ValidateSessionTransitionSaveAndAdmin(map);
    }

    private static void ValidateConnections(DungeonMap map)
    {
        for (int index = 0; index < map.Rooms.Count - 1; index++)
        {
            Need(map.Rooms[index].Connections.Contains(map.Rooms[index + 1].Id) &&
                map.Rooms[index + 1].Connections.Contains(map.Rooms[index].Id),
                $"Map 7 rooms {index} and {index + 1} are not connected both ways.");
        }
    }

    private static void ValidateContinuousRoute(DungeonMap map)
    {
        int prior = int.MaxValue;
        for (int x = map.WorldBounds.Left + 4;
            x < map.WorldBounds.Right - 4;
            x += 8)
        {
            int top = MandatoryGroundTop(map, x);
            Need(top != int.MaxValue,
                $"Map 7 main route has a gap near X={x}.");
            if (prior != int.MaxValue)
            {
                Need(Math.Abs(top - prior) <= 24,
                    $"Map 7 main route has an unsafe step near X={x}.");
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
            map.StartRoom.Bounds.Left + 105f,
            size,
            map,
            0f);
        var player = new DungeonAscendant.Player.Player(start);
        float target = map.ExitRoom.Bounds.Right - 155f;
        float priorX = player.Position.X;
        int stillFrames = 0;

        for (int frame = 0;
            frame < 13000 && player.Position.X < target;
            frame++)
        {
            bool jump = stillFrames >= 8 && player.IsGrounded;
            player.UpdateSideScrollingMovement(
                Frame,
                new KeyboardState(Keys.D),
                jump,
                map);
            stillFrames = player.Position.X <= priorX + .05f
                ? stillFrames + 1
                : 0;
            priorX = player.Position.X;
        }

        Need(player.Position.X >= target,
            "Player A/D + Space simulation could not traverse Map 7.");
        Need(map.WorldBounds.Contains(player.Bounds),
            "Map 7 traversal left the world bounds.");
    }

    private static void ValidateCheckpoints(DungeonMap map)
    {
        Vector2 size = new(
            DungeonAscendant.Player.Player.BodyHurtboxWidth,
            DungeonAscendant.Player.Player.BodyHurtboxHeight);
        var ids = new HashSet<string>();
        foreach (SanctumCheckpointDefinition checkpoint in map.SanctumCheckpoints)
        {
            Need(ids.Add(checkpoint.CheckpointId),
                $"Duplicate Map 7 checkpoint {checkpoint.CheckpointId}.");
            DemonKingSanctumZoneDefinition zone = map.DemonKingSanctumZones
                .Single(candidate => candidate.ZoneId == checkpoint.ZoneId);
            Need(zone.Bounds.Contains(checkpoint.Position.ToPoint()),
                $"Map 7 checkpoint {checkpoint.CheckpointId} left its sector.");
            Need(SideScrollingCollision.IsPositionFree(
                    checkpoint.Position, size, map) &&
                SideScrollingCollision.IsSupported(
                    checkpoint.Position, size, map),
                $"Map 7 checkpoint {checkpoint.CheckpointId} is unsafe.");
            foreach (SpawnSocket socket in map.SpawnSockets)
            {
                Need(Vector2.DistanceSquared(
                        checkpoint.Position, socket.Position) >= 240f * 240f,
                    $"Map 7 checkpoint {checkpoint.CheckpointId} overlaps " +
                    $"neutral socket {socket.SocketId}.");
            }
        }
    }

    private static void ValidateSocketsAndEnemies(DungeonMap map)
    {
        var ids = new HashSet<string>();
        var placementSize = new Vector2(80f, 110f);
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            Need(ids.Add(socket.SocketId),
                $"Duplicate Map 7 socket {socket.SocketId}.");
            bool combat = socket.SocketId.StartsWith(
                "map7-enemy-", StringComparison.Ordinal) || socket.Role == SpawnSocketRole.Boss;
            if (combat)
                Need(socket.SuggestedEnemyTypes.Count == 1 &&
                    socket.Role is SpawnSocketRole.GroundMelee or
                        SpawnSocketRole.Ranged or SpawnSocketRole.Heavy or
                        SpawnSocketRole.Flying or SpawnSocketRole.Elite or SpawnSocketRole.Boss &&
                    socket.IntroEligible,
                    $"Map 7 combat socket {socket.SocketId} is incomplete.");
            else
                Need(socket.Role == SpawnSocketRole.Unassigned &&
                    socket.SuggestedEnemyTypes.Count == 0 &&
                    !socket.IntroEligible,
                    $"Future Map 7 socket {socket.SocketId} was assigned early.");
            EncounterZone encounter = map.EncounterZones.Single(space =>
                combat ? space.ZoneId == socket.ZoneId :
                    space.SectionId == socket.ZoneId);
            DemonKingSanctumZoneDefinition zone = map.DemonKingSanctumZones
                .Single(candidate => candidate.ZoneId == encounter.SectionId);
            Need(zone.Bounds.Contains(socket.Position.ToPoint()) &&
                zone.Bounds.Intersects(socket.PlacementBounds),
                $"Map 7 socket {socket.SocketId} left its authored sector.");
            Vector2 safe = SideScrollingCollision.ResolveSafeEntrySpawn(
                socket.Position.X,
                placementSize,
                map,
                0f);
            Need(socket.PlacementBounds.Contains(safe.ToPoint()) &&
                SideScrollingCollision.IsPositionFree(
                    safe, placementSize, map) &&
                SideScrollingCollision.IsSupported(
                    safe, placementSize, map),
                $"Map 7 socket {socket.SocketId} has no safe floor/headroom.");

            Need(encounter.ActivationBounds.Contains(safe.ToPoint()),
                $"Map 7 socket {socket.SocketId} left encounter bounds.");
            if (combat)
                Need(encounter.SuggestedEnemyTypes.Contains(
                        socket.SuggestedEnemyTypes[0]) &&
                    encounter.MaxConcurrentEnemies ==
                        (encounter.ZoneId ==
                            "sanctum-broken-vows-encounter" ? 2 : 1),
                    $"Map 7 encounter {encounter.ZoneId} roster is invalid.");
            else
                Need(socket.SuggestedEnemyTypes.Count == 0 &&
                    socket.Role == SpawnSocketRole.Unassigned,
                    $"Future Map 7 socket {socket.SocketId} is not inert.");
        }
        Need(Enum.GetValues<EnemyType>()[^1] == EnemyType.Vaelthar,
            "Map 7 pass registered enemies beyond 71.");
        ValidateEnemyBehavior(map);
        ValidatePenitentAndScribe(map);
        ValidateReliquaryAndWraith(map);
        DemonKingSanctumEliteValidation.ValidateOrThrow();
        ThroneboundSeraphValidation.ValidateOrThrow();
        VaeltharValidation.ValidateOrThrow();
    }

    private static void ValidateEnemyBehavior(DungeonMap map)
    {
        SpawnSocket sentinelSocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.OathboundSentinel));
        SpawnSocket acolyteSocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.VowshardAcolyte));
        var manager = new EnemyManager(7);
        Enemy sentinel = manager.SpawnAuthoredEnemy(
            EnemyType.OathboundSentinel, sentinelSocket,
            map.FindRoomContaining(sentinelSocket.Position), map, 10, 3);
        Enemy acolyte = manager.SpawnAuthoredEnemy(
            EnemyType.VowshardAcolyte, acolyteSocket,
            map.FindRoomContaining(acolyteSocket.Position), map, 10, 3);
        Need(sentinel is DemonKingSanctumEnemy &&
            acolyte is DemonKingSanctumEnemy &&
            sentinel.MaxPoise > acolyte.MaxPoise &&
            sentinel.MaxPoise < 150f && acolyte.MaxPoise < 80f,
            $"Map 7 enemy factory or finite poise tuning failed: " +
            $"sentinel={sentinel?.GetType().Name}/{sentinel?.MaxPoise}, " +
            $"acolyte={acolyte?.GetType().Name}/{acolyte?.MaxPoise}.");
        Need(AudioProfiles.GetEnemy(EnemyType.OathboundSentinel).Id ==
                "oathbound_sentinel" &&
            AudioProfiles.GetEnemy(EnemyType.VowshardAcolyte).Id ==
                "vowshard_acolyte",
            "Map 7 enemy audio profiles do not resolve.");
        IReadOnlyDictionary<string, string> en = Loc.GetStrings(Language.English);
        IReadOnlyDictionary<string, string> vi = Loc.GetStrings(Language.Vietnamese);
        Need(en["enemy.oathbound_sentinel.name"] == "OATHBOUND SENTINEL" &&
            en["enemy.vowshard_acolyte.name"] == "VOWSHARD ACOLYTE" &&
            vi["enemy.oathbound_sentinel.name"] == "VỆ BINH KHẾ ƯỚC" &&
            vi["enemy.vowshard_acolyte.name"] == "TÍN ĐỒ MẢNH THỆ",
            "Map 7 enemy localization is incomplete.");

        var projectileManager = new Combat.ProjectileManager();
        var player = new DungeonAscendant.Player.Player(new Vector2(
            acolyte.Position.X - 280f, acolyte.Position.Y));
        bool sawShard = false, sawMarker = false, markerExpired = false;
        for (int frame = 0; frame < 1500; frame++)
        {
            acolyte.Update(Frame, player, map, projectileManager);
            projectileManager.Update(Frame, player, map);
            sawShard |= projectileManager.Projectiles.Any(projectile =>
                projectile.Type == Combat.ProjectileType.Vowshard &&
                projectile.LockedTarget == null &&
                !projectile.PenetratesTerrain);
            var sanctum = (DemonKingSanctumEnemy)acolyte;
            sawMarker |= sanctum.HasFracturedOathMarker;
            if (sawMarker && !sanctum.HasFracturedOathMarker)
                markerExpired = true;
        }
        Need(sawShard && sawMarker && markerExpired &&
            projectileManager.Projectiles.Count <= 3,
            "Acolyte projectiles/telegraphed marker did not remain finite.");

        var closePlayer = new DungeonAscendant.Player.Player(new Vector2(
            sentinel.Position.X - 98f, sentinel.Position.Y));
        var noProjectiles = new Combat.ProjectileManager();
        for (int frame = 0; frame < 900; frame++)
            sentinel.Update(Frame, closePlayer, map, noProjectiles);
        Need(((DemonKingSanctumEnemy)sentinel).CombatState !=
                SanctumEnemyState.OathThrustActive &&
            noProjectiles.Projectiles.Count == 0,
            "Sentinel attack states failed to terminate cleanly.");

        var director = new WildForestEncounterDirector();
        director.Reset(map);
        EncounterZone encounter = map.EncounterZones[0];
        var encounterManager = new EnemyManager(9);
        encounterManager.Reset(map, 10, 3, RegionType.DemonKingSanctuary,
            suppressProceduralSpawns: true);
        Vector2 trigger = encounter.ActivationBounds.Center.ToVector2();
        for (int frame = 0; frame < 40; frame++)
            director.Update(Frame, trigger, map, encounterManager, 10, 3);
        int spawned = encounterManager.Enemies.Count;
        for (int frame = 0; frame < 40; frame++)
            director.Update(Frame, trigger, map, encounterManager, 10, 3);
        Need(spawned == 2 && encounterManager.Enemies.Count == spawned,
            "Hall of Broken Vows encounter did not spawn exactly once.");
    }

    private static void ValidatePenitentAndScribe(DungeonMap map)
    {
        SpawnSocket huskSocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.PenitentHusk));
        SpawnSocket scribeSocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.AbyssScribe));
        var manager = new EnemyManager(17);
        Enemy huskBase = manager.SpawnAuthoredEnemy(
            EnemyType.PenitentHusk, huskSocket,
            map.FindRoomContaining(huskSocket.Position), map, 10, 3);
        Enemy scribeBase = manager.SpawnAuthoredEnemy(
            EnemyType.AbyssScribe, scribeSocket,
            map.FindRoomContaining(scribeSocket.Position), map, 10, 3);
        Need(huskBase is DemonKingSanctumPenitentEnemy &&
            scribeBase is DemonKingSanctumPenitentEnemy &&
            huskBase.MaxPoise > scribeBase.MaxPoise &&
            huskBase.MaxPoise < 180f,
            "Enemy 64/65 factory or finite poise tuning failed.");
        Need(AudioProfiles.GetEnemy(EnemyType.PenitentHusk).Id ==
                "penitent_husk" &&
            AudioProfiles.GetEnemy(EnemyType.AbyssScribe).Id ==
                "abyss_scribe",
            "Enemy 64/65 audio profiles do not resolve.");
        IReadOnlyDictionary<string, string> en = Loc.GetStrings(Language.English);
        IReadOnlyDictionary<string, string> vi = Loc.GetStrings(Language.Vietnamese);
        Need(en["enemy.penitent_husk.name"] == "PENITENT HUSK" &&
            en["enemy.abyss_scribe.name"] == "ABYSS SCRIBE" &&
            vi["enemy.penitent_husk.name"] == "XÁC TỘI ĐỒ" &&
            vi["enemy.abyss_scribe.name"] == "THƯ LẠI VỰC THẲM",
            "Enemy 64/65 localization is incomplete.");

        var husk = (DemonKingSanctumPenitentEnemy)huskBase;
        var huskPlayer = new DungeonAscendant.Player.Player(new Vector2(
            husk.Position.X - 96f, husk.Position.Y));
        var noProjectiles = new Combat.ProjectileManager();
        bool sawBrace = false, finiteBracePoise = false;
        int unchangedStateFrames = 0;
        PenitentCombatState priorState = husk.CombatState;
        for (int frame = 0; frame < 1600; frame++)
        {
            husk.Update(Frame, huskPlayer, map, noProjectiles);
            if (husk.CombatState == priorState) unchangedStateFrames++;
            else { priorState = husk.CombatState; unchangedStateFrames = 0; }
            Need(unchangedStateFrames < 130,
                "Penitent Husk attack state failed to terminate.");
            if (husk.IsRepentanceBraced && !sawBrace)
            {
                sawBrace = true;
                float before = husk.CurrentPoise;
                husk.ReceiveDamage(1, 50f);
                float loss = before - husk.CurrentPoise;
                finiteBracePoise = loss > 0f && loss < 50f;
            }
        }
        Need(sawBrace && finiteBracePoise && !husk.IsRepentanceBraced &&
            noProjectiles.Projectiles.Count == 0,
            "Penitent Husk brace is missing, permanent, or full super armor.");

        var scribe = (DemonKingSanctumPenitentEnemy)scribeBase;
        var scribePlayer = new DungeonAscendant.Player.Player(new Vector2(
            scribe.Position.X - 280f, scribe.Position.Y));
        var projectiles = new Combat.ProjectileManager();
        bool sawScript = false, sawMark = false, markExpired = false;
        bool sawLine = false, lineExpired = false;
        int scribeStateFrames = 0;
        PenitentCombatState priorScribeState = scribe.CombatState;
        for (int frame = 0; frame < 1900; frame++)
        {
            scribe.Update(Frame, scribePlayer, map, projectiles);
            projectiles.Update(Frame, scribePlayer, map);
            if (scribe.CombatState == priorScribeState) scribeStateFrames++;
            else
            {
                priorScribeState = scribe.CombatState;
                scribeStateFrames = 0;
            }
            Need(scribeStateFrames < 130,
                "Abyss Scribe attack state failed to terminate.");
            sawScript |= projectiles.Projectiles.Any(projectile =>
                projectile.Type == Combat.ProjectileType.AbyssScript &&
                projectile.LockedTarget == null &&
                !projectile.PenetratesTerrain);
            sawMark |= scribe.HasCondemnedMark;
            sawLine |= scribe.HasForbiddenLine;
            if (sawMark && !scribe.HasCondemnedMark) markExpired = true;
            if (sawLine && !scribe.HasForbiddenLine) lineExpired = true;
        }
        Need(sawScript && sawMark && markExpired && sawLine && lineExpired &&
            projectiles.Projectiles.Count <= 1,
            "Abyss Scribe projectile, mark, or line is not finite/readable.");
        var cleanup = (DemonKingSanctumPenitentEnemy)EnemyFactory.Create(
            EnemyType.AbyssScribe,
            new EnemySpawnContext(scribe.Position, 10, 3, scribe.RoomId,
                false, GoblinVariant.Normal));
        bool cleanupHazardStarted = false;
        for (int frame = 0; frame < 900 && !cleanupHazardStarted; frame++)
        {
            cleanup.Update(Frame, scribePlayer, map,
                new Combat.ProjectileManager());
            cleanupHazardStarted = cleanup.HasCondemnedMark ||
                cleanup.HasForbiddenLine;
        }
        Need(cleanupHazardStarted,
            "Abyss Scribe cleanup fixture did not create a glyph hazard.");
        cleanup.ReceiveDamage(cleanup.CurrentHealth + 1, 999f);
        Need(!cleanup.HasCondemnedMark && !cleanup.HasForbiddenLine,
            "Abyss Scribe retained glyph hazards after death.");

        ValidateSingleSpawn(map, "sanctum-penitent-descent",
            EnemyType.PenitentHusk);
        ValidateSingleSpawn(map, "sanctum-abyss-scribe-altar",
            EnemyType.AbyssScribe);
    }

    private static void ValidateReliquaryAndWraith(DungeonMap map)
    {
        SpawnSocket reliquarySocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.GraveReliquary));
        SpawnSocket wraithSocket = map.SpawnSockets.Single(socket =>
            socket.SuggestedEnemyTypes.Contains(EnemyType.CloisterWraith));
        var manager = new EnemyManager(31);
        Enemy reliquaryBase = manager.SpawnAuthoredEnemy(
            EnemyType.GraveReliquary, reliquarySocket,
            map.FindRoomContaining(reliquarySocket.Position), map, 10, 3);
        Enemy wraithBase = manager.SpawnAuthoredEnemy(
            EnemyType.CloisterWraith, wraithSocket,
            map.FindRoomContaining(wraithSocket.Position), map, 10, 3);
        Need(reliquaryBase is DemonKingSanctumReliquaryEnemy &&
            wraithBase is DemonKingSanctumReliquaryEnemy &&
            !reliquaryBase.IsFlying && wraithBase.IsFlying &&
            reliquaryBase.MaxPoise > wraithBase.MaxPoise &&
            reliquaryBase.MaxPoise < 180f,
            "Enemy 66/67 factory, movement class, or finite poise failed.");
        Need(AudioProfiles.GetEnemy(EnemyType.GraveReliquary).Id ==
                "grave_reliquary" &&
            AudioProfiles.GetEnemy(EnemyType.CloisterWraith).Id ==
                "cloister_wraith",
            "Enemy 66/67 audio profiles do not resolve.");
        IReadOnlyDictionary<string, string> en = Loc.GetStrings(Language.English);
        IReadOnlyDictionary<string, string> vi = Loc.GetStrings(Language.Vietnamese);
        Need(en["enemy.grave_reliquary.name"] == "GRAVE RELIQUARY" &&
            en["enemy.cloister_wraith.name"] == "CLOISTER WRAITH" &&
            vi["enemy.grave_reliquary.name"] ==
                "TH\u00c1NH T\u00cdCH M\u1ed8 V\u1ef0C" &&
            vi["enemy.cloister_wraith.name"] ==
                "O\u00c1N LINH TU VI\u1ec6N",
            "Enemy 66/67 localization is incomplete.");

        var reliquary = (DemonKingSanctumReliquaryEnemy)reliquaryBase;
        EncounterZone reliquaryEncounter = map.EncounterZones.Single(zone =>
            zone.ZoneId == "sanctum-grave-reliquary-kings");
        reliquary.SetAuthoredEncounterBounds(
            reliquaryEncounter.ActivationBounds);
        var closePlayer = new DungeonAscendant.Player.Player(new Vector2(
            reliquary.Position.X - 96f, reliquary.Position.Y))
        {
            DebugGodMode = true,
        };
        var noProjectiles = new Combat.ProjectileManager();
        bool sawGuard = false, finiteGuard = false;
        bool guardCooldownExpired = false;
        bool sawCleave = false, sawDrag = false;
        bool sawBurst = false, burstExpired = false;
        int unchangedFrames = 0;
        ReliquaryCombatState previous = reliquary.CombatState;
        for (int frame = 0; frame < 1900; frame++)
        {
            reliquary.Update(Frame, closePlayer, map, noProjectiles);
            if (reliquary.CombatState == previous) unchangedFrames++;
            else { previous = reliquary.CombatState; unchangedFrames = 0; }
            Need(unchangedFrames < 130,
                "Grave Reliquary attack state failed to terminate.");
            sawCleave |= reliquary.AttackKind ==
                ReliquaryAttackKind.SepulchralCleave;
            sawDrag |= reliquary.AttackKind == ReliquaryAttackKind.BoneDrag;
            if (reliquary.IsReliquaryBurstActive)
            {
                sawBurst = true;
                Need(reliquary.ActiveAttackArea.Width <= 136 &&
                    reliquary.ActiveAttackArea.Height <= 136,
                    "Grave Reliquary burst exceeded its compact radius.");
            }
            else if (sawBurst) burstExpired = true;
            if (reliquary.IsOssuaryGuarding && !sawGuard)
            {
                sawGuard = true;
                float before = reliquary.CurrentPoise;
                reliquary.ReceiveDamageFrom(1, 50f, closePlayer.Position);
                float loss = before - reliquary.CurrentPoise;
                finiteGuard = loss > 0f && loss < 50f &&
                    reliquary.GuardCooldownRemaining > 0f;
            }
            if (sawGuard && !reliquary.IsOssuaryGuarding &&
                reliquary.GuardCooldownRemaining <= 0f)
                guardCooldownExpired = true;
        }
        Need(sawCleave && sawDrag && sawGuard && finiteGuard &&
            guardCooldownExpired && !reliquary.IsOssuaryGuarding &&
            sawBurst && burstExpired && noProjectiles.Projectiles.Count == 0,
            "Grave Reliquary attacks, guard, or burst are missing, unbounded, " +
            "or permanent.");

        var reliquaryCleanup =
            (DemonKingSanctumReliquaryEnemy)EnemyFactory.Create(
                EnemyType.GraveReliquary,
                new EnemySpawnContext(reliquary.Position, 10, 3,
                    reliquary.RoomId, false, GoblinVariant.Normal));
        bool cleanupBurstStarted = false;
        for (int frame = 0; frame < 1200 && !cleanupBurstStarted; frame++)
        {
            reliquaryCleanup.Update(
                Frame, closePlayer, map, noProjectiles);
            cleanupBurstStarted = reliquaryCleanup.IsReliquaryBurstActive;
        }
        Need(cleanupBurstStarted,
            "Grave Reliquary cleanup fixture did not activate its cursed burst.");
        reliquaryCleanup.ReceiveDamage(
            reliquaryCleanup.CurrentHealth + 1, 999f);
        Need(!reliquaryCleanup.IsReliquaryBurstActive &&
            reliquaryCleanup.CombatState == ReliquaryCombatState.Ready,
            "Grave Reliquary retained its cursed burst after death.");

        var wraith = (DemonKingSanctumReliquaryEnemy)wraithBase;
        EncounterZone wraithEncounter = map.EncounterZones.Single(zone =>
            zone.ZoneId == "sanctum-cloister-wraith");
        wraith.SetAuthoredEncounterBounds(wraithEncounter.ActivationBounds);
        Vector2 wraithPlayerPosition =
            SideScrollingCollision.ResolveSafeEntrySpawn(
                wraith.Position.X - 280f,
                new Vector2(
                    DungeonAscendant.Player.Player.BodyHurtboxWidth,
                    DungeonAscendant.Player.Player.BodyHurtboxHeight),
                map,
                0f);
        var wraithPlayer = new DungeonAscendant.Player.Player(
            wraithPlayerPosition)
        {
            DebugGodMode = true,
        };
        bool sawPhase = false, sawGrasp = false, graspExpired = false;
        bool sawClaw = false, sawEcho = false, echoExpired = false;
        int wraithStateFrames = 0;
        ReliquaryCombatState priorWraithState = wraith.CombatState;
        for (int frame = 0; frame < 2200; frame++)
        {
            wraith.Update(Frame, wraithPlayer, map, noProjectiles);
            if (wraith.CombatState == priorWraithState) wraithStateFrames++;
            else
            {
                priorWraithState = wraith.CombatState;
                wraithStateFrames = 0;
            }
            Need(wraithStateFrames < 130,
                "Cloister Wraith attack state failed to terminate.");
            sawClaw |= wraith.AttackKind == ReliquaryAttackKind.WraithClaw;
            if (wraith.CombatState is ReliquaryCombatState.VeilPassTelegraph or
                ReliquaryCombatState.VeilPassMove or
                ReliquaryCombatState.VeilPassRecovery)
            {
                sawPhase = true;
                Need(wraith.VeilPassDistance <=
                        DemonKingSanctumReliquaryEnemy.MaximumVeilPassDistance + .1f &&
                    wraithEncounter.ActivationBounds.Contains(
                        wraith.VeilPassDestination.ToPoint()),
                    "Cloister Wraith phase left its short authored bounds.");
            }
            sawGrasp |= wraith.HasCloisterGrasp;
            if (wraith.HasCloisterGrasp)
                Need(wraithEncounter.ActivationBounds.Contains(
                        wraith.CloisterGraspPosition.ToPoint()),
                    "Cloister Grasp marker left the authored encounter.");
            if (sawGrasp && !wraith.HasCloisterGrasp) graspExpired = true;
            sawEcho |= wraith.HasEcho;
            if (sawEcho && !wraith.HasEcho)
            {
                echoExpired = true;
                Need(wraith.EchoActivationCount == wraith.EchoCreatedCount,
                    "A completed Wraith echo did not strike exactly once.");
            }
        }
        Need(sawPhase && wraith.PhaseCooldownRemaining >= 0f &&
            sawClaw && sawGrasp && graspExpired && sawEcho && echoExpired &&
            wraith.EchoCreatedCount > 0 &&
            wraith.EchoActivationCount > 0 &&
            wraith.EchoActivationCount <= wraith.EchoCreatedCount,
            "Cloister Wraith phase, grasp, or single echo lifecycle failed.");

        var cleanup = (DemonKingSanctumReliquaryEnemy)EnemyFactory.Create(
            EnemyType.CloisterWraith,
            new EnemySpawnContext(wraith.Position, 10, 3, wraith.RoomId,
                false, GoblinVariant.Normal));
        bool ownedEffectStarted = false;
        for (int frame = 0; frame < 1000 && !ownedEffectStarted; frame++)
        {
            cleanup.Update(Frame, wraithPlayer, map, noProjectiles);
            ownedEffectStarted = cleanup.HasCloisterGrasp || cleanup.HasEcho;
        }
        Need(ownedEffectStarted,
            "Cloister Wraith cleanup fixture did not create an owned effect.");
        cleanup.ReceiveDamage(cleanup.CurrentHealth + 1, 999f);
        Need(!cleanup.HasCloisterGrasp && !cleanup.HasEcho,
            "Cloister Wraith retained a grasp or echo after death.");

        ValidateSingleSpawn(map, "sanctum-grave-reliquary-kings",
            EnemyType.GraveReliquary);
        ValidateSingleSpawn(map, "sanctum-cloister-wraith",
            EnemyType.CloisterWraith);
        ValidateReliquaryPairTraversalAndLeashes(map);
    }

    private static void ValidateReliquaryPairTraversalAndLeashes(DungeonMap map)
    {
        var manager = new EnemyManager(67);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary,
            suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector();
        director.Reset(map);
        EncounterZone reliquaryEncounter = map.EncounterZones.Single(zone =>
            zone.ZoneId == "sanctum-grave-reliquary-kings");
        EncounterZone wraithEncounter = map.EncounterZones.Single(zone =>
            zone.ZoneId == "sanctum-cloister-wraith");

        TriggerEncounter(director, manager, map, reliquaryEncounter);
        Enemy reliquary = manager.Enemies.Single(enemy =>
            enemy.Type == EnemyType.GraveReliquary);
        TriggerEncounter(director, manager, map, wraithEncounter);
        Enemy wraith = manager.Enemies.Single(enemy =>
            enemy.Type == EnemyType.CloisterWraith);
        Need(reliquary.IsAlive && wraith.IsAlive &&
            director.IsTriggered(reliquaryEncounter.ZoneId) &&
            director.IsTriggered(wraithEncounter.ZoneId),
            "Enemy 67 was gated by the still-living Enemy 66 encounter.");

        ValidateEnemyOnlyLeash(reliquary, reliquaryEncounter, map);
        ValidateEnemyOnlyLeash(wraith, wraithEncounter, map);
    }

    private static void TriggerEncounter(
        WildForestEncounterDirector director,
        EnemyManager manager,
        DungeonMap map,
        EncounterZone encounter)
    {
        Vector2 trigger = new(
            encounter.ActivationBounds.Left + 24f,
            encounter.ActivationBounds.Center.Y);
        for (int frame = 0; frame < 40; frame++)
            director.Update(Frame, trigger, map, manager, 10, 3);
    }

    private static void ValidateEnemyOnlyLeash(
        Enemy enemy,
        EncounterZone encounter,
        DungeonMap map)
    {
        var player = new DungeonAscendant.Player.Player(new Vector2(
            encounter.ActivationBounds.Right + 180f,
            enemy.Position.Y))
        {
            DebugGodMode = true
        };
        Vector2 playerBefore = player.Position;
        enemy.Update(Frame, player, map, new Combat.ProjectileManager());
        Need(enemy.IsReturningToAuthoredHome && player.Position == playerBefore,
            $"{enemy.Type} leash did not disengage without moving the Player.");
    }

    private static void ValidateSingleSpawn(
        DungeonMap map, string encounterId, EnemyType type)
    {
        EncounterZone encounter = map.EncounterZones.Single(zone =>
            zone.ZoneId == encounterId);
        var manager = new EnemyManager(23);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary,
            suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector();
        director.Reset(map);
        SpawnSocket socket = map.SpawnSockets.Single(candidate =>
            candidate.SuggestedEnemyTypes.Contains(type));
        float left = encounter.ActivationBounds.Left + 24f;
        float right = encounter.ActivationBounds.Right - 24f;
        float triggerX = MathF.Abs(left - socket.Position.X) >=
            MathF.Abs(right - socket.Position.X) ? left : right;
        Vector2 trigger = new(triggerX, encounter.ActivationBounds.Center.Y);
        for (int frame = 0; frame < 40; frame++)
            director.Update(Frame, trigger, map, manager, 10, 3);
        int count = manager.Enemies.Count(enemy => enemy.Type == type);
        for (int frame = 0; frame < 40; frame++)
            director.Update(Frame, trigger, map, manager, 10, 3);
        Need(count == 1 && manager.Enemies.Count(enemy => enemy.Type == type) == 1,
            $"{type} did not spawn exactly once per encounter lifecycle.");
    }

    private static void ValidateBossArenaAndEndpoint(DungeonMap map)
    {
        DemonKingSanctumZoneDefinition arena = map.DemonKingSanctumZones[8];
        DungeonRoom arenaRoom = map.FindRoomContaining(
            arena.Bounds.Center.ToVector2());
        Need(arena.Kind == DemonKingSanctumZoneKind.ThroneSanctum &&
            arena.Bounds.Width >= 2200 && arena.Bounds.Height >= 600,
            "Map 7 throne arena lacks combat and camera clearance.");
        Need(arenaRoom.Platforms.All(platform =>
                platform.Kind != PlatformKind.Raised),
            "Map 7 throne arena contains raised collision traps.");
        Need(map.SanctumProps.Any(prop =>
                prop.ZoneId == arena.ZoneId &&
                prop.Kind == SanctumPropKind.Throne),
            "Map 7 throne arena lacks its environmental centerpiece.");

        DemonKingSanctumZoneDefinition endpoint =
            map.DemonKingSanctumZones[9];
        Need(endpoint.Kind == DemonKingSanctumZoneKind.FinalEndpoint &&
            map.AuthoredExitPosition.HasValue &&
            endpoint.Bounds.Contains(map.AuthoredExitPosition.Value.ToPoint()) &&
            SideScrollingCollision.IsPositionFree(
                map.AuthoredExitPosition.Value,
                new Vector2(40f, 56f),
                map) &&
            SideScrollingCollision.IsSupported(
                map.AuthoredExitPosition.Value,
                new Vector2(40f, 56f),
                map),
            "Map 7 final endpoint is not structurally safe.");
    }

    private static void ValidateResources(DungeonMap map)
    {
        IReadOnlyDictionary<string, string> english =
            Loc.GetStrings(Language.English);
        IReadOnlyDictionary<string, string> vietnamese =
            Loc.GetStrings(Language.Vietnamese);
        Need(english.ContainsKey("hud.map_demon_king_sanctum") &&
            vietnamese.ContainsKey("hud.map_demon_king_sanctum"),
            "Map 7 title localization is incomplete.");
        foreach (DemonKingSanctumZoneDefinition zone in
            map.DemonKingSanctumZones)
        {
            Need(english.ContainsKey(zone.NameKey) &&
                vietnamese.ContainsKey(zone.NameKey),
                $"Map 7 localization is missing {zone.NameKey}.");
        }
        WorldSoundEvent[] hooks =
        {
            WorldSoundEvent.Map7StoneStep,
            WorldSoundEvent.Map7TempleResonance,
            WorldSoundEvent.Map7RitualHum,
            WorldSoundEvent.Map7AbyssWind,
            WorldSoundEvent.Map7ThroneAmbience
        };
        foreach (WorldSoundEvent hook in hooks)
        {
            Need(AudioProfiles.GetWorld(hook) != null,
                $"Map 7 semantic audio hook {hook} does not resolve.");
        }
    }

    private static void ValidateSessionTransitionSaveAndAdmin(DungeonMap map)
    {
        string directory = Path.Combine(Path.GetTempPath(),
            "DungeonAscendant-map7-foundation-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");
        try
        {
            DungeonMap map6 = new DemonCityMapGenerator().Generate();
            var manager = new SaveSlotManager(path);
            Need(manager.Store(new SaveSlotData
            {
                SlotIndex = 0,
                Occupied = true,
                CharacterName = "SANCTUM TEST",
                Appearance =
                    DungeonAscendant.Player.CharacterAppearance.CreateDefault(),
                Level = 9,
                DungeonDepth = 6,
                CurrentMapId = "map-06",
                CurrentArea = "SEALED SANCTUARY WAY",
                CheckpointX = map6.AuthoredExitPosition.Value.X,
                CheckpointY = map6.AuthoredExitPosition.Value.Y,
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
                BossDefeated = true,
                LastPlayedUtc = DateTimeOffset.UtcNow
            }), "Map 7 transition fixture could not create a save.");

            var session = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 7707,
                savePath: path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            Need(session.DungeonDepth == 6 && session.CurrentDungeon.IsDemonCity,
                "Map 7 transition fixture did not restore Map 6.");
            session.Player.MoveTo(session.ExitPosition);
            Press(session, Keys.E);
            Need(session.DungeonDepth == 7 &&
                session.CurrentDungeon.IsDemonKingSanctum &&
                session.CurrentRegion == RegionType.DemonKingSanctuary &&
                session.Map06ExitPrepared && session.Player.Level == 9 &&
                session.Enemies.Enemies.Count == 0 && !session.ShouldRenderBoss,
                "Map 6 to Map 7 transition lost registration or Player state.");
            Need(SideScrollingCollision.IsPositionFree(
                    session.Player.Position,
                    session.Player.Size,
                    session.CurrentDungeon) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Player.Bounds) &&
                session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
                "Map 7 transition produced an unsafe Player/camera spawn.");

            for (int zone = 0;
                zone < session.CurrentDungeon.DemonKingSanctumZones.Count;
                zone++)
            {
                session.ExecuteDeveloperCommand(
                    DeveloperCommand.TeleportZone,
                    7,
                    zone);
                Need(session.GetCurrentZoneIndex() == zone &&
                    SideScrollingCollision.IsPositionFree(
                        session.Player.Position,
                        session.Player.Size,
                        session.CurrentDungeon) &&
                    session.CurrentDungeon.WorldBounds.Contains(
                        session.Camera.ViewBounds) &&
                    session.Enemies.Enemies.Count == 0,
                    $"Map 7 admin zone {zone} arrival is invalid.");
            }

            SanctumCheckpointDefinition checkpoint =
                session.CurrentDungeon.SanctumCheckpoints[2];
            session.Player.MoveTo(checkpoint.Position);
            session.FlushActiveSave();
            SaveSlotData map7Slot = new SaveSlotManager(path).GetSlot(0);
            Need(map7Slot.DungeonDepth == 7 &&
                map7Slot.CurrentMapId == "map-07" &&
                map7Slot.Map06ExitPrepared,
                "Map 7 did not persist its map ID or Map 6 transition state.");
            var restored = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 7708,
                savePath: path);
            Press(restored, Keys.Enter);
            Press(restored, Keys.Enter);
            Need(restored.DungeonDepth == 7 &&
                restored.CurrentDungeon.IsDemonKingSanctum &&
                Vector2.DistanceSquared(
                    restored.Player.Position, checkpoint.Position) < 1f &&
                restored.Enemies.Enemies.Count == 0,
                "Map 7 checkpoint/save mapping did not restore safely.");

            restored.ExecuteDeveloperCommand(
                DeveloperCommand.TeleportMap,
                7,
                0);
            Need(restored.DungeonDepth == 7 &&
                restored.CurrentDungeon.IsDemonKingSanctum &&
                restored.CurrentMapName == Loc.Get("hud.map_demon_king_sanctum"),
                "Map 7 F10/admin map entry is not registered.");
            Press(restored, Keys.F10);
            Need(restored.DeveloperPanel.IsOpen &&
                restored.DeveloperPanel.TargetMap == 7 &&
                restored.DeveloperPanel.GetMapName().StartsWith("MAP 7") &&
                restored.DeveloperPanel.GetZoneCount() ==
                    DemonKingSanctumMapGenerator.ZoneCount,
                "Map 7 is missing from the live F10 map/zone selector.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static int MandatoryGroundTop(DungeonMap map, int x)
    {
        int result = int.MaxValue;
        foreach (Platform platform in map.Platforms)
        {
            if (platform.Kind != PlatformKind.Ground ||
                x < platform.Bounds.Left || x >= platform.Bounds.Right)
                continue;
            result = Math.Min(result, platform.Bounds.Top);
        }
        return result;
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame, new KeyboardState(key), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
