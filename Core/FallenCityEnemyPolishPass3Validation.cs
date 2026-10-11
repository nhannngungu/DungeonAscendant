using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

public static class FallenCityEnemyPolishPass3Validation
{
    public static void ValidateOrThrow(DungeonMap map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        ValidateIdentityEnvelope(map);
        ValidateWatcherAlarmFlow(map);
        ValidateCityAlarmHandshake(map);
        ValidateExecutionerCombat(map);
        ValidateIntroductions(map);
    }

    private static void ValidateIdentityEnvelope(DungeonMap map)
    {
        Need(
            FallenCityWatcherExecutionerSpriteRenderer.WatcherVisualSize ==
                new Vector2(84f, 76f),
            "Bell Watcher must retain an upright, human support silhouette.");
        Need(
            FallenCityWatcherExecutionerSpriteRenderer.ExecutionerVisualSize.X >=
                128f &&
            FallenCityWatcherExecutionerSpriteRenderer.ExecutionerVisualSize.Y <
                EnemyVisualProfile.DeathKnight.VisualSize.Y,
            "Street Executioner must read as broad human mass, not a tall Death Knight.");

        FallenCityEnemy watcher = Create(
            EnemyType.BellWatcher,
            map,
            500f);
        FallenCityEnemy militia = Create(
            EnemyType.FallenMilitiaman,
            map,
            700f);
        FallenCityEnemy executioner = Create(
            EnemyType.StreetExecutioner,
            map,
            900f);
        FallenCityEnemy plague = Create(
            EnemyType.PlagueCarrier,
            map,
            1050f);
        Need(watcher.MaxHealth < militia.MaxHealth &&
            watcher.MaxPoise < militia.MaxPoise,
            "Bell Watcher direct durability must remain below the Militiaman.");
        Need(executioner.MaxHealth > plague.MaxHealth &&
            executioner.MaxPoise > plague.MaxPoise &&
            executioner.MovementSpeed < militia.MovementSpeed,
            "Street Executioner lost its heavy durability and limited-mobility identity.");
    }

    private static void ValidateWatcherAlarmFlow(DungeonMap map)
    {
        FallenCityEnemy watcher = Create(
            EnemyType.BellWatcher,
            map,
            500f);
        PlayerCharacter player = CreatePlayer(map, 660f);
        watcher.ConfigureAlarmTarget(watcher.Position);
        var projectiles = new ProjectileManager();

        Tick(watcher, player, map, projectiles, .01f);
        Need(watcher.CombatState == FallenCityCombatState.WatcherNotice &&
            watcher.IsCombatAttackTelegraphing &&
            !watcher.TryConsumeAlarmSignal(),
            "Bell Watcher did not visibly notice the Player before signaling.");
        Tick(watcher, player, map, projectiles,
            FallenCityEnemy.WatcherNoticeSeconds + .08f);
        Need(watcher.CombatState == FallenCityCombatState.WatcherRetreat,
            "Bell Watcher did not create distance after noticing the Player.");
        Tick(watcher, player, map, projectiles,
            FallenCityEnemy.WatcherRetreatSeconds + .08f);
        Need(watcher.CombatState ==
                FallenCityCombatState.WatcherAlarmWindup &&
            watcher.IsAlarmWindup &&
            !watcher.TryConsumeAlarmSignal(),
            "Alarm Signal skipped its interruptible windup.");

        watcher.ReceiveDamage(1);
        Need(watcher.IsAlarmInterrupted &&
            !watcher.TryConsumeAlarmSignal(),
            "Damaging the Watcher did not interrupt its pending signal.");
        Tick(watcher, player, map, projectiles,
            FallenCityEnemy.AlarmInterruptedSeconds - .10f);
        Need(!watcher.TryConsumeAlarmSignal(),
            "Interrupted alarm leaked a delayed signal.");

        FallenCityEnemy fallback = Create(
            EnemyType.BellWatcher,
            map,
            760f);
        PlayerCharacter fallbackPlayer = CreatePlayer(map, 940f);
        fallback.ConfigureAlarmTarget(null);
        bool sawWindup = false;
        bool sawSignalMotion = false;
        for (int frame = 0; frame < 120; frame++)
        {
            Tick(fallback, fallbackPlayer, map, projectiles, .05f);
            sawWindup |= fallback.IsAlarmWindup;
            sawSignalMotion |= fallback.IsAlarmSignalActive;
            if (fallback.HasSignaledAlarm)
                break;
        }
        Need(sawWindup && sawSignalMotion && fallback.HasSignaledAlarm,
            "Fallback horn call failed without an alarm object.");
        Need(fallback.TryConsumeAlarmSignal() &&
            !fallback.TryConsumeAlarmSignal(),
            "One alarm animation produced more than one signal event.");

        FallenCityEnemy close = Create(
            EnemyType.BellWatcher,
            map,
            1010f);
        PlayerCharacter closePlayer = CreatePlayer(map, 1058f);
        Tick(close, closePlayer, map, new ProjectileManager(), .01f);
        Tick(close, closePlayer, map, new ProjectileManager(),
            FallenCityEnemy.WatcherNoticeSeconds + .04f);
        PlayerCharacter pursuingPlayer = CreatePlayer(map, close.Position.X);
        Tick(close, pursuingPlayer, map, new ProjectileManager(),
            FallenCityEnemy.WatcherRetreatSeconds + .08f);
        // Once pressured again at close range, the Watcher must use its weak
        // backup attack rather than dealing support-enemy burst damage.
        bool sawPanicSlash = false;
        for (int frame = 0; frame < 20; frame++)
        {
            Tick(close, pursuingPlayer, map, new ProjectileManager(), .05f);
            sawPanicSlash |= close.BellWatcherAction ==
                BellWatcherActionKind.PanicSlash;
            if (sawPanicSlash)
                break;
        }
        Need(sawPanicSlash,
            "Bell Watcher close-pressure response is missing Panic Slash.");
    }

    private static void ValidateCityAlarmHandshake(DungeonMap map)
    {
        SpawnSocket watcherSocket = null;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            foreach (EnemyType type in socket.SuggestedEnemyTypes)
            {
                if (type == EnemyType.BellWatcher)
                {
                    watcherSocket = socket;
                    break;
                }
            }
            if (watcherSocket != null)
                break;
        }
        Need(watcherSocket != null,
            "No authored Bell Watcher socket exists for alarm integration.");

        DungeonRoom room = map.FindRoomContaining(watcherSocket.Position);
        var manager = new EnemyManager(7703);
        manager.Reset(
            map,
            4,
            1,
            RegionType.RuinedCity,
            suppressProceduralSpawns: true);
        Enemy spawned = manager.SpawnAuthoredEnemy(
            EnemyType.BellWatcher,
            watcherSocket,
            room,
            map,
            4,
            1);
        Need(spawned is FallenCityEnemy,
            "Authored Bell Watcher could not spawn for alarm validation.");

        var player = new PlayerCharacter(new Vector2(
            spawned.Position.X + 175f,
            room.GroundY - PlayerCharacter.BodyHurtboxHeight / 2f))
        {
            DebugGodMode = true
        };
        var alarm = new CityAlarmSystem();
        var encounters = new WildForestEncounterDirector();
        encounters.Reset(map);
        var projectiles = new ProjectileManager();
        var roots = new RootHazardManager();
        alarm.Update(
            Frame(.05f),
            map,
            player.Position,
            manager,
            encounters,
            4,
            1);
        var authoredWatcher = (FallenCityEnemy)spawned;
        Need(authoredWatcher.HasAlarmTarget,
            "Bell Watcher did not acquire its active zone alarm position.");
        for (int frame = 0; frame < 180 && alarm.Level == 0; frame++)
        {
            alarm.Update(
                Frame(.05f),
                map,
                player.Position,
                manager,
                encounters,
                4,
                1);
            manager.UpdateCombatAndAi(
                Frame(.05f),
                player,
                map,
                projectiles,
                roots);
        }
        Need(alarm.Level == 1 &&
            alarm.WatcherSignalFeedbackTimeRemaining > 0f,
            "Successful Watcher signal did not raise City Alarm with feedback.");

        for (int frame = 0; frame < 30; frame++)
        {
            alarm.Update(
                Frame(.05f),
                map,
                player.Position,
                manager,
                encounters,
                4,
                1);
            manager.UpdateCombatAndAi(
                Frame(.05f),
                player,
                map,
                projectiles,
                roots);
        }
        Need(alarm.Level == 1,
            "Completed Watcher signal increased City Alarm repeatedly.");
        Need(alarm.TryDisableNearest(
                authoredWatcher.AlarmTargetPosition,
                map),
            "Authored alarm object could not be disabled for fallback validation.");
        alarm.Update(
            Frame(.05f),
            map,
            player.Position,
            manager,
            encounters,
            4,
            1);
        Need(!authoredWatcher.HasAlarmTarget,
            "Bell Watcher continued targeting a disabled alarm object.");
    }

    private static void ValidateExecutionerCombat(DungeonMap map)
    {
        FallenCityEnemy executioner = Create(
            EnemyType.StreetExecutioner,
            map,
            500f);
        PlayerCharacter player = CreatePlayer(map, 580f);
        var projectiles = new ProjectileManager();
        Tick(executioner, player, map, projectiles, .01f);
        Need(executioner.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.HeavyCleave &&
            executioner.CombatState ==
                FallenCityCombatState.ExecutionerWindup,
            "Street Executioner did not open with readable Heavy Cleave.");
        int healthBefore = player.CurrentHealth;
        Tick(executioner, player, map, projectiles,
            FallenCityEnemy.HeavyCleaveWindupSeconds - .12f);
        Need(player.CurrentHealth == healthBefore,
            "Heavy Cleave dealt damage before the visible weapon arc.");
        Tick(executioner, player, map, projectiles, .20f);
        Need(player.CurrentHealth < healthBefore,
            "Heavy Cleave active arc did not resolve its wide hitbox.");
        Tick(executioner, player, map, projectiles, .22f);
        Need(executioner.CombatState ==
                FallenCityCombatState.ExecutionerRecovery,
            "Heavy Cleave did not expose a long punishable recovery.");

        bool sawSlam = false;
        bool sawSweep = false;
        bool sawImpact = false;
        for (int frame = 0; frame < 240; frame++)
        {
            Tick(executioner, player, map, projectiles, .05f);
            sawSlam |= executioner.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.ExecutionSlam;
            sawSweep |= executioner.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.DraggingSweep;
            sawImpact |= executioner.ExecutionerImpactProgress > 0f;
        }
        Need(sawSlam && sawSweep && sawImpact,
            "Execution Slam, Dragging Sweep, or ground impact feedback is missing.");

        FallenCityEnemy enraged = Create(
            EnemyType.StreetExecutioner,
            map,
            760f);
        PlayerCharacter durable = CreatePlayer(map, 840f);
        enraged.ReceiveDamage((int)MathF.Ceiling(enraged.MaxHealth * .70f));
        Tick(enraged, durable, map, new ProjectileManager(), .01f);
        Need(enraged.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.EnragedExecution &&
            enraged.CombatState == FallenCityCombatState.ExecutionerWindup,
            "Bounded low-health Enraged Execution cycle did not begin.");
        bool enrageFinished = false;
        bool enrageCycleComplete = false;
        bool repeatedEnrage = false;
        for (int frame = 0; frame < 180; frame++)
        {
            Tick(enraged, durable, map, new ProjectileManager(), .05f);
            if (enrageCycleComplete && enraged.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.EnragedExecution)
            {
                repeatedEnrage = true;
            }
            enrageFinished |= enraged.CombatState ==
                FallenCityCombatState.ExecutionerRecovery;
            enrageCycleComplete |= enrageFinished &&
                enraged.StreetExecutionerAttack ==
                    StreetExecutionerAttackKind.None;
        }
        Need(enrageFinished && !repeatedEnrage,
            "Enraged Execution repeated instead of remaining one bounded cycle.");

        enraged.ReceiveDamage(int.MaxValue);
        enraged.UpdateDeathPresentation(Frame(.70f));
        Need(!enraged.IsDeathPresentationComplete,
            "Executioner collapse vanished before its heavy fall could read.");
        enraged.UpdateDeathPresentation(Frame(.70f));
        Need(enraged.IsDeathPresentationComplete,
            "Executioner collapse did not settle cleanly.");
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        FallenCityEnemy watcher = Create(EnemyType.BellWatcher, map, 500f);
        var watcherIntro = new EnemyIntroductionManager();
        watcherIntro.Update(
            Frame(.01f), watcher.Position, new Enemy[] { watcher }, null, false);
        Need(watcherIntro.Active?.Name == "BELL WATCHER" &&
            watcherIntro.Active.Title == "VOICE OF THE ALARM",
            "Bell Watcher first-encounter introduction changed.");

        FallenCityEnemy executioner = Create(
            EnemyType.StreetExecutioner,
            map,
            760f);
        var executionerIntro = new EnemyIntroductionManager();
        executionerIntro.Update(
            Frame(.01f),
            executioner.Position,
            new Enemy[] { executioner },
            null,
            false);
        Need(executionerIntro.Active?.Name == "STREET EXECUTIONER" &&
            executionerIntro.Active.Title == "THE LAST SENTENCE",
            "Street Executioner first-encounter introduction changed.");
    }

    private static FallenCityEnemy Create(
        EnemyType type,
        DungeonMap map,
        float x)
    {
        var enemy = new FallenCityEnemy(
            type,
            Vector2.Zero,
            1,
            1,
            map.StartRoom.Id,
            false);
        enemy.PlaceAtAuthoredSocket(
            new Vector2(x, map.StartRoom.GroundY),
            aerial: false);
        return enemy;
    }

    private static PlayerCharacter CreatePlayer(DungeonMap map, float x)
    {
        return new PlayerCharacter(new Vector2(
            x,
            map.StartRoom.GroundY -
                PlayerCharacter.BodyHurtboxHeight / 2f))
        {
            DebugGodMode = true
        };
    }

    private static void Tick(
        FallenCityEnemy enemy,
        PlayerCharacter player,
        DungeonMap map,
        ProjectileManager projectiles,
        float seconds)
    {
        var roots = new RootHazardManager();
        float remaining = seconds;
        while (remaining > 0f)
        {
            float step = MathF.Min(.05f, remaining);
            enemy.Update(Frame(step), player, map, projectiles, roots);
            remaining -= step;
        }
    }

    private static GameTime Frame(float seconds) => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(seconds));

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(
                $"Fallen City enemy polish pass 3 validation failed: {message}");
    }
}
