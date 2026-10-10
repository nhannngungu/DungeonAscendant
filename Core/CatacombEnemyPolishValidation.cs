using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

/// <summary>
/// Headless contracts for the first Ancient Catacombs enemy polish pass.
/// These checks keep animation tells, damage windows, release frames, cleanup,
/// and the shared seven-weapon status hooks from drifting apart.
/// </summary>
public static class CatacombEnemyPolishValidation
{
    public static void ValidateOrThrow()
    {
        ValidateProfilesAndCollisionHierarchy();
        ValidateSkeletonCombatClock();
        ValidateArcherReleaseAndInterrupt();
        ValidateRottenCorpseCombatAndDeathWarning();
        ValidateWraithStateLoop();
        ValidateUndeadGuardCombatAndGuardBreak();
        ValidateGraveBatHarassmentLoop();
        ValidatePassThreeIntroductions();
        ValidateCursedKnightCombatAndArmorBreak();
        ValidateSoulCollectorControlLoop();
        ValidatePassFourIntroductions();
        ValidateDeathAndFallenKnightPolish();
        ValidateIntroductionsAndDeathCleanup();
    }

    private static void ValidateProfilesAndCollisionHierarchy()
    {
        Require(EnemyVisualProfile.Skeleton.VisualSize ==
                new Vector2(76f, 68f),
            "Skeleton procedural envelope changed.");
        Require(EnemyVisualProfile.SkeletonArcher.VisualSize ==
                new Vector2(88f, 72f),
            "Skeleton Archer procedural envelope changed.");
        Require(!EnemyVisualProfile.Skeleton
                .GetAnimation(EnemyVisualState.Death).IsLooping &&
            !EnemyVisualProfile.SkeletonArcher
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Skeleton deaths must remain deterministic one-shots.");

        var skeleton = Create(EnemyType.Skeleton, Vector2.Zero);
        var archer = Create(EnemyType.SkeletonArcher, Vector2.Zero);
        Require(skeleton.Size == new Vector2(40f, 56f) &&
            archer.Size == new Vector2(36f, 58f),
            "Skeleton hurtboxes no longer match their readable core bodies.");
        Require(archer.Size.X < skeleton.Size.X &&
            EnemyVisualProfile.SkeletonArcher.VisualSize.X >
                archer.Size.X * 2f,
            "Archer cloak and bow must not inflate its collision body.");
    }

    private static void ValidateSkeletonCombatClock()
    {
        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[1];
        float x = room.Bounds.Left + 430f;
        var skeleton = Create(
            EnemyType.Skeleton,
            new Vector2(x, room.GroundY - 28f),
            room.Id);
        skeleton.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 60f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        Tick(skeleton, player, map, projectiles, .01f);
        Require(skeleton.CombatState == CatacombCombatState.MeleeWindup &&
            skeleton.SkeletonAttack == SkeletonAttackKind.RustedSlash &&
            skeleton.IsCombatAttackTelegraphing &&
            !skeleton.IsCombatAttackActive,
            "Skeleton slash must begin with a non-damaging visible tell.");
        Tick(skeleton, player, map, projectiles,
            CatacombEnemy.SlashWindupSeconds + .02f);
        Require(skeleton.CombatState == CatacombCombatState.MeleeActive &&
            skeleton.IsCombatAttackActive,
            "Skeleton sword arc did not align with its active window.");
        Tick(skeleton, player, map, projectiles,
            CatacombEnemy.SlashActiveSeconds + .02f);
        Require(skeleton.CombatState == CatacombCombatState.MeleeRecovery &&
            !skeleton.IsCombatAttackActive,
            "Skeleton slash remained dangerous after the weapon passed.");

        bool sawReturnCut = false;
        bool sawOverhead = false;
        for (int frame = 0; frame < 550; frame++)
        {
            Tick(skeleton, player, map, projectiles, .05f);
            sawReturnCut |= skeleton.SkeletonAttack ==
                SkeletonAttackKind.ReturnCut;
            sawOverhead |= skeleton.SkeletonAttack ==
                SkeletonAttackKind.OverheadChop;
        }
        Require(sawReturnCut && sawOverhead,
            "Controlled return-cut and overhead patterns are not reachable.");

        skeleton.AddArcaneImprint();
        Require(skeleton.ArcaneImprintCount == 1,
            "Spellblade imprint compatibility regressed.");
        skeleton.ApplyControl(.5f, 1f);
        skeleton.ApplyKnockback(player.Position, 12f, map);
        skeleton.ApplyPoiseDamage(skeleton.MaxPoise);
        Require(skeleton.IsStaggered &&
            skeleton.CombatState == CatacombCombatState.Ready,
            "Breaker/Raider control must interrupt the fragile skeleton.");
    }

    private static void ValidateArcherReleaseAndInterrupt()
    {
        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[2];
        float x = room.Bounds.Left + 680f;
        var archer = Create(
            EnemyType.SkeletonArcher,
            new Vector2(x, room.GroundY - 29f),
            room.Id);
        archer.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x - 260f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        Tick(archer, player, map, projectiles, .01f);
        Require(archer.CombatState == CatacombCombatState.ArcherNock &&
            projectiles.Projectiles.Count == 0,
            "Archer must reach for an arrow before firing.");
        Tick(archer, player, map, projectiles,
            CatacombEnemy.ArcherNockSeconds + .02f);
        Require(archer.CombatState == CatacombCombatState.ArcherDraw &&
            archer.IsCombatAttackTelegraphing &&
            projectiles.Projectiles.Count == 0,
            "Archer draw must be visible and projectile-free.");

        archer.ApplyPoiseDamage(archer.MaxPoise);
        Require(archer.IsStaggered &&
            archer.CombatState == CatacombCombatState.Ready,
            "Archer stagger did not cancel its draw.");
        Tick(archer, player, map, projectiles, 1f);
        Require(projectiles.Projectiles.Count == 0,
            "An interrupted Archer fired an invisible delayed arrow.");

        var firingArcher = Create(
            EnemyType.SkeletonArcher,
            new Vector2(x, room.GroundY - 29f),
            room.Id);
        firingArcher.PlaceAtAuthoredSocket(
            new Vector2(x, room.GroundY), false);
        Tick(firingArcher, player, map, projectiles, .01f);
        Tick(firingArcher, player, map, projectiles,
            CatacombEnemy.ArcherNockSeconds + .02f);
        Tick(firingArcher, player, map, projectiles,
            CatacombEnemy.StandardDrawSeconds + .02f);
        Require(firingArcher.CombatState ==
                CatacombCombatState.ArcherRelease &&
            projectiles.Projectiles.Count == 1,
            "Standard arrow was not created on the visible release frame.");
        Projectile arrow = projectiles.Projectiles[0];
        Require(arrow.Type == ProjectileType.Arrow && arrow.Blockable &&
            MathF.Abs(arrow.Velocity.Y) < .01f &&
            Vector2.Distance(
                arrow.SourcePosition,
                firingArcher.GetArrowReleasePosition()) < .1f,
            "Archer arrow origin, horizontal lane, or block behavior regressed.");

        var heavyArcher = Create(
            EnemyType.SkeletonArcher,
            new Vector2(x, room.GroundY - 29f),
            room.Id);
        heavyArcher.PlaceAtAuthoredSocket(
            new Vector2(x, room.GroundY), false);
        var heavyProjectiles = new ProjectileManager();
        bool sawHeavyArrow = false;
        for (int frame = 0; frame < 650 && !sawHeavyArrow; frame++)
        {
            Tick(heavyArcher, player, map, heavyProjectiles, .05f);
            foreach (Projectile projectile in heavyProjectiles.Projectiles)
            {
                if (!projectile.EmpoweredVisual)
                    continue;
                sawHeavyArrow = projectile.Damage > arrow.Damage &&
                    projectile.Velocity.Length() > ProjectileManager.ArrowSpeed;
            }
        }
        Require(sawHeavyArrow,
            "Long-tell heavy arrow pattern or stronger impact is unreachable.");

        var closeArcher = Create(
            EnemyType.SkeletonArcher,
            new Vector2(x, room.GroundY - 29f),
            room.Id);
        closeArcher.PlaceAtAuthoredSocket(
            new Vector2(x, room.GroundY), false);
        player.MoveTo(new Vector2(x - 55f, room.GroundY - 28f));
        Tick(closeArcher, player, map, new ProjectileManager(), .01f);
        Require(closeArcher.CombatState ==
                CatacombCombatState.BowShoveWindup &&
            closeArcher.IsCombatAttackTelegraphing,
            "Point-blank Archer response must be a bow shove, not a shot.");
        float beforeShove = player.Position.X;
        Tick(closeArcher, player, map, new ProjectileManager(),
            CatacombEnemy.BowShoveWindupSeconds + .07f);
        Require(player.Position.X < beforeShove,
            "Bow shove did not create its small collision-safe pushback.");
    }

    private static void ValidateIntroductionsAndDeathCleanup()
    {
        var skeleton = Create(EnemyType.Skeleton, new Vector2(120f, 120f));
        var archer = Create(
            EnemyType.SkeletonArcher,
            new Vector2(140f, 120f));
        var introductions = new EnemyIntroductionManager();
        introductions.Update(
            Frame(.05f),
            Vector2.Zero,
            new Enemy[] { skeleton },
            null,
            false);
        Require(introductions.Active?.Name == "SKELETON" &&
            introductions.Active.Title == "THE RESTLESS DEAD" &&
            introductions.Active.PresentationStrength == 1,
            "Skeleton first encounter copy or intensity changed.");
        Expire(introductions, skeleton);
        introductions.Update(
            Frame(.05f), Vector2.Zero, new Enemy[] { skeleton }, null, false);
        Require(!introductions.IsPresenting,
            "Second Skeleton repeated its first encounter presentation.");
        introductions.Update(
            Frame(.05f), Vector2.Zero, new Enemy[] { archer }, null, false);
        Require(introductions.Active?.Name == "SKELETON ARCHER" &&
            introductions.Active.Title == "THE SILENT WATCHER" &&
            introductions.Active.PresentationStrength == 1,
            "Skeleton Archer first encounter copy or intensity changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        var manager = new EnemyManager(91);
        manager.Reset(
            map, 1, 1, RegionType.WildForest,
            suppressProceduralSpawns: true);
        DungeonRoom room = map.Rooms[1];
        SpawnSocket socket = map.SpawnSockets[0];
        Enemy spawned = manager.SpawnAuthoredEnemy(
            EnemyType.Skeleton,
            socket,
            map.FindRoomContaining(socket.Position) ?? room,
            map,
            1,
            1);
        if (spawned != null)
        {
            spawned.ReceiveDamage(int.MaxValue);
            var defeated = new List<Enemy>();
            manager.RemoveDefeated(defeated, out _);
            Require(manager.DefeatPresentations.Count == 1,
                "Skeleton death vanished before its bone pile presentation.");
            manager.UpdateCombatAndAi(
                Frame(.6f),
                new PlayerCharacter(Vector2.Zero),
                map,
                new ProjectileManager(),
                new RootHazardManager());
            Require(manager.DefeatPresentations.Count == 0,
                "Skeleton death presentation did not clean up once.");
        }
    }

    private static void ValidateRottenCorpseCombatAndDeathWarning()
    {
        Require(EnemyVisualProfile.RottenCorpse.VisualSize ==
                new Vector2(92f, 78f) &&
            !EnemyVisualProfile.RottenCorpse
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Rotten Corpse silhouette or one-shot death profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[3];
        float x = room.Bounds.Left + 430f;
        var corpse = Create(
            EnemyType.RottenCorpse,
            new Vector2(x, room.GroundY - 34f),
            room.Id);
        corpse.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 58f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        Tick(corpse, player, map, projectiles, .01f);
        Require(corpse.CombatState == CatacombCombatState.CorpseWindup &&
            corpse.RottenCorpseAttack == RottenCorpseAttackKind.Grab &&
            corpse.IsCombatAttackTelegraphing &&
            !corpse.IsCombatAttackActive,
            "Rotten Corpse grab must begin with a readable safe windup.");

        bool sawGrab = false;
        bool sawSlam = false;
        bool sawVomit = false;
        for (int frame = 0; frame < 520; frame++)
        {
            Tick(corpse, player, map, projectiles, .05f);
            sawGrab |= corpse.RottenCorpseAttack ==
                RottenCorpseAttackKind.Grab;
            sawSlam |= corpse.RottenCorpseAttack ==
                RottenCorpseAttackKind.BodySlam;
            sawVomit |= corpse.RottenCorpseAttack ==
                RottenCorpseAttackKind.RotVomit;
        }
        Require(sawGrab && sawSlam && sawVomit,
            "Rotten Corpse grab, body slam, and short rot-vomit cycle is incomplete.");
        Require(corpse.MaxHealth > Create(
                EnemyType.Skeleton, Vector2.Zero).MaxHealth &&
            corpse.MaxPoise > Create(
                EnemyType.Skeleton, Vector2.Zero).MaxPoise,
            "Rotten Corpse must remain tougher and steadier than a Skeleton.");

        corpse.ReceiveDamage(int.MaxValue);
        corpse.UpdateDeathPresentation(Frame(.89f));
        Require(!corpse.IsDeathPresentationComplete,
            "Rotten Corpse disappeared before its delayed warning could resolve.");
        corpse.UpdateDeathPresentation(Frame(.34f));
        Require(corpse.IsDeathPresentationComplete &&
            RottenCorpseBurst.WarningSeconds >= .8f &&
            RottenCorpseBurst.WarningSeconds <= 1f,
            "Rotten Corpse warning or collapse duration is outside the authored window.");
    }

    private static void ValidateWraithStateLoop()
    {
        Require(EnemyVisualProfile.Wraith.VisualSize ==
                new Vector2(88f, 82f) &&
            !EnemyVisualProfile.Wraith
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Wraith silhouette or dissolution profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[4];
        float x = room.Bounds.Left + 520f;
        var wraith = Create(
            EnemyType.Wraith,
            new Vector2(x, room.GroundY - 27f),
            room.Id);
        wraith.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 62f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        int health = wraith.CurrentHealth;
        wraith.ReceiveDamage(10);
        Require(wraith.IsEthereal && !wraith.CanBeTargeted &&
            wraith.CurrentHealth == health &&
            !wraith.IsCombatAttackActive,
            "Ethereal Wraith must reposition without attacking or taking full damage.");

        Tick(wraith, player, map, projectiles,
            CatacombEnemy.WraithEtherealSeconds + .03f);
        Require(!wraith.IsEthereal && wraith.CanBeTargeted &&
            wraith.CombatState == CatacombCombatState.WraithMaterialize &&
            wraith.IsCombatAttackTelegraphing,
            "Wraith materialization must be deterministic, visible, and vulnerable.");

        bool sawClaw = false;
        bool sawDash = false;
        bool sawWave = false;
        bool attackedWhileEthereal = false;
        bool returnedToEthereal = false;
        for (int frame = 0; frame < 620; frame++)
        {
            Tick(wraith, player, map, projectiles, .05f);
            attackedWhileEthereal |= wraith.IsEthereal &&
                wraith.IsCombatAttackActive;
            sawClaw |= wraith.WraithAttack == WraithAttackKind.SpectralClaw;
            sawDash |= wraith.WraithAttack == WraithAttackKind.SpectralDash;
            sawWave |= wraith.WraithAttack == WraithAttackKind.CurseWave;
            returnedToEthereal |= wraith.IsEthereal &&
                wraith.SpectralPhase == WraithPhase.Ethereal;
        }
        Require(!attackedWhileEthereal && returnedToEthereal &&
            sawClaw && sawDash && sawWave,
            "Wraith phase loop or bounded three-attack pattern regressed.");
        Require(map.WorldBounds.Contains(wraith.Position.ToPoint()),
            "Wraith repositioning crossed a critical map boundary.");
    }

    private static void ValidateUndeadGuardCombatAndGuardBreak()
    {
        Require(EnemyVisualProfile.UndeadGuard.VisualSize ==
                new Vector2(94f, 78f) &&
            !EnemyVisualProfile.UndeadGuard
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Undead Guard silhouette or collapse profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[5];
        float x = room.Bounds.Left + 470f;
        var guard = Create(
            EnemyType.UndeadGuard,
            new Vector2(x, room.GroundY - 31f),
            room.Id);
        guard.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 68f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        Tick(guard, player, map, projectiles, .01f);
        Require(guard.IsGuarding &&
            guard.CombatState == CatacombCombatState.GuardStance,
            "Undead Guard did not establish its frontal shield stance.");
        int guardedHealth = guard.CurrentHealth;
        guard.ReceiveDamageFrom(20, 10f, player.Position);
        int guardedLoss = guardedHealth - guard.CurrentHealth;

        var rearGuard = Create(
            EnemyType.UndeadGuard,
            new Vector2(x, room.GroundY - 31f),
            room.Id);
        int rearHealth = rearGuard.CurrentHealth;
        rearGuard.ReceiveDamageFrom(20, 10f, new Vector2(x - 80f, 0f));
        int rearLoss = rearHealth - rearGuard.CurrentHealth;
        Require(guardedLoss > 0 && guardedLoss < rearLoss,
            "Guard shield must reduce, not erase, frontal light damage.");

        guard.ReceiveDamageFrom(42, 48f, player.Position);
        Require(guard.IsGuardBroken && !guard.IsGuarding,
            "Strong frontal impact did not break the Guard's shield stance.");
        Tick(guard, player, map, projectiles,
            CatacombEnemy.GuardBreakSeconds + .08f);
        Require(!guard.IsGuardBroken,
            "Guard break did not end in a deterministic vulnerable window.");

        var attacker = Create(
            EnemyType.UndeadGuard,
            new Vector2(x, room.GroundY - 31f),
            room.Id);
        attacker.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        bool sawStrike = false;
        bool sawBash = false;
        bool sawHeavy = false;
        for (int frame = 0; frame < 620; frame++)
        {
            Tick(attacker, player, map, projectiles, .05f);
            sawStrike |= attacker.UndeadGuardAttack ==
                UndeadGuardAttackKind.GuardedStrike;
            sawBash |= attacker.UndeadGuardAttack ==
                UndeadGuardAttackKind.ShieldBash;
            sawHeavy |= attacker.UndeadGuardAttack ==
                UndeadGuardAttackKind.HeavyCleave;
        }
        Require(sawStrike && sawBash && sawHeavy,
            "Undead Guard's strike, bash, and committed heavy cycle is incomplete.");
        Require(attacker.MaxPoise > Create(
                EnemyType.Skeleton, Vector2.Zero).MaxPoise,
            "Undead Guard Poise must remain above a normal Skeleton.");
    }

    private static void ValidateGraveBatHarassmentLoop()
    {
        Require(EnemyVisualProfile.GraveBat.VisualSize ==
                new Vector2(84f, 48f) &&
            !EnemyVisualProfile.GraveBat
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Grave Bat silhouette or physical death profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[4];
        float x = room.Bounds.Left + 580f;
        var bat = Create(
            EnemyType.GraveBat,
            new Vector2(x, room.GroundY - 150f),
            room.Id);
        bat.PlaceAtAuthoredSocket(
            new Vector2(x, room.GroundY - 150f), true);
        var player = new PlayerCharacter(
            new Vector2(x + 55f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        bool sawScreech = false;
        bool sawClaw = false;
        bool sawFeint = false;
        bool sawPressure = false;
        float largestFrameMovement = 0f;
        Vector2 previous = bat.Position;
        for (int frame = 0; frame < 650; frame++)
        {
            Tick(bat, player, map, projectiles, .05f);
            sawScreech |= bat.GraveBatAttack ==
                GraveBatAttackKind.GraveScreech;
            sawClaw |= bat.GraveBatAttack == GraveBatAttackKind.ClawPass;
            sawFeint |= bat.GraveBatAttack == GraveBatAttackKind.SwarmFeint;
            sawPressure |= bat.GraveBatStaminaPressureMultiplier < 1f;
            largestFrameMovement = MathF.Max(
                largestFrameMovement,
                Vector2.Distance(previous, bat.Position));
            previous = bat.Position;
            Require(SideScrollingCollision.IsPositionFree(
                    bat.Position, bat.Size, map),
                "Grave Bat entered a wall, ceiling, or world boundary.");
        }
        Require(sawScreech && sawClaw && sawFeint && sawPressure,
            "Grave Bat's screech, claw pass, feint, or stamina pressure is missing.");
        Require(largestFrameMovement < 20f,
            "Grave Bat repositioned with an unreadable teleport-sized step.");

        bat.ReceiveDamage(int.MaxValue);
        bat.UpdateDeathPresentation(Frame(.45f));
        Require(!bat.IsDeathPresentationComplete &&
            !float.IsNaN(bat.GraveBatDeathLandingY),
            "Grave Bat death skipped its physical fall window.");
        bat.UpdateDeathPresentation(Frame(.65f));
        Require(bat.IsDeathPresentationComplete,
            "Grave Bat physical death presentation did not clean up.");
    }

    private static void ValidatePassThreeIntroductions()
    {
        var guard = Create(EnemyType.UndeadGuard, new Vector2(80f, 80f));
        var guardIntroductions = new EnemyIntroductionManager();
        guardIntroductions.Update(
            Frame(.05f), Vector2.Zero, new Enemy[] { guard }, null, false);
        Require(guardIntroductions.Active?.Name == "UNDEAD GUARD" &&
            guardIntroductions.Active.Title == "KEEPER OF THE DEAD" &&
            guardIntroductions.Active.Description ==
                "EVEN IN DEATH IT STILL GUARDS WHAT LIES BEYOND",
            "Undead Guard first encounter copy changed.");

        var bat = Create(EnemyType.GraveBat, new Vector2(90f, 80f));
        var batIntroductions = new EnemyIntroductionManager();
        batIntroductions.Update(
            Frame(.05f), Vector2.Zero, new Enemy[] { bat }, null, false);
        Require(batIntroductions.Active?.Name == "GRAVE BAT" &&
            batIntroductions.Active.Title == "WHISPER OF THE CRYPT" &&
            batIntroductions.Active.Description ==
                "ITS CRY DRAINS THE STRENGTH FROM THOSE WHO STILL BREATHE",
            "Grave Bat first encounter copy changed.");
    }

    private static void ValidateCursedKnightCombatAndArmorBreak()
    {
        Require(EnemyVisualProfile.CursedKnight.VisualSize ==
                new Vector2(116f, 102f) &&
            !EnemyVisualProfile.CursedKnight
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Cursed Knight elite silhouette or collapse profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[6];
        float x = room.Bounds.Left + 650f;
        var knight = Create(
            EnemyType.CursedKnight,
            new Vector2(x, room.GroundY - 35f),
            room.Id);
        knight.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 72f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        Tick(knight, player, map, projectiles, .05f);
        Require(knight.CombatState == CatacombCombatState.KnightDormant &&
            !knight.IsCombatAttackActive,
            "Cursed Knight must wake from a non-damaging dormant pose.");

        bool sawSweep = false;
        bool sawExecution = false;
        bool sawThrust = false;
        for (int frame = 0; frame < 720; frame++)
        {
            float targetX = frame < 420 ? x + 72f : x + 155f;
            player.MoveTo(new Vector2(targetX, room.GroundY - 28f));
            Tick(knight, player, map, projectiles, .05f);
            sawSweep |= knight.CursedKnightAttack ==
                CursedKnightAttackKind.CursedSweep;
            sawExecution |= knight.CursedKnightAttack ==
                CursedKnightAttackKind.ExecutionStrike;
            sawThrust |= knight.CursedKnightAttack ==
                CursedKnightAttackKind.AdvancingThrust;
        }
        Require(sawSweep && sawExecution && sawThrust,
            "Cursed Knight sweep, execution, or advancing thrust is unreachable.");

        var armorKnight = Create(
            EnemyType.CursedKnight,
            new Vector2(x, room.GroundY - 35f), room.Id);
        float poiseBefore = armorKnight.CurrentPoise;
        armorKnight.ReceiveDamageFrom(10, 20f, player.Position);
        Require(armorKnight.CurrentPoise > poiseBefore - 20f,
            "Intact Cursed Armor did not resist light Poise damage.");
        armorKnight.ApplyPoiseDamage(armorKnight.MaxPoise);
        Require(armorKnight.IsCursedArmorBroken && armorKnight.IsStaggered,
            "Poise break did not flare and expose Cursed Armor.");
        int healthBefore = armorKnight.CurrentHealth;
        armorKnight.ReceiveDamageFrom(20, 0f, player.Position);
        Require(healthBefore - armorKnight.CurrentHealth > 20,
            "Broken Cursed Armor did not create its vulnerable damage window.");
    }

    private static void ValidateSoulCollectorControlLoop()
    {
        Require(EnemyVisualProfile.SoulCollector.VisualSize ==
                new Vector2(104f, 104f) &&
            !EnemyVisualProfile.SoulCollector
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Soul Collector elite silhouette or soul-collapse profile changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom room = map.Rooms[7];
        float x = room.Bounds.Left + 720f;
        var collector = Create(
            EnemyType.SoulCollector,
            new Vector2(x, room.GroundY - 34f), room.Id);
        collector.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(x + 260f, room.GroundY - 28f));
        var projectiles = new ProjectileManager();

        bool sawFieldTell = false;
        bool sawField = false;
        bool sawBolt = false;
        bool sawPull = false;
        Vector2 previousPlayerPosition = player.Position;
        float largestPull = 0f;
        for (int frame = 0; frame < 620; frame++)
        {
            Tick(collector, player, map, projectiles, .05f);
            sawFieldTell |= collector.SoulCollectorAttack ==
                    SoulCollectorAttackKind.RitualCurseField &&
                !collector.RitualPreviewBounds.IsEmpty;
            sawField |= collector.HasActiveSoulField &&
                collector.SoulFieldBounds.Width <= 200;
            sawPull |= collector.SoulCollectorAttack ==
                SoulCollectorAttackKind.GravePull;
            foreach (Projectile projectile in projectiles.Projectiles)
            {
                if (projectile.Type != ProjectileType.SoulBolt)
                    continue;
                sawBolt = projectile.CursePressure > 0f &&
                    projectile.Velocity.Length() < ProjectileManager.ArrowSpeed;
            }
            largestPull = MathF.Max(
                largestPull,
                Vector2.Distance(previousPlayerPosition, player.Position));
            previousPlayerPosition = player.Position;
        }
        Require(sawFieldTell && sawField && sawBolt && sawPull,
            "Soul Collector field, bolt, or delayed pull cycle is incomplete.");
        Require(SideScrollingCollision.IsPositionFree(
                player.Position, player.Size, map) &&
            largestPull <= 32f,
            "Grave Pull crossed terrain or dragged the Player too far.");

        var closeCollector = Create(
            EnemyType.SoulCollector,
            new Vector2(x, room.GroundY - 34f), room.Id);
        closeCollector.PlaceAtAuthoredSocket(new Vector2(x, room.GroundY), false);
        player.MoveTo(new Vector2(x + 62f, room.GroundY - 28f));
        Tick(closeCollector, player, map, new ProjectileManager(), .05f);
        Require(closeCollector.SoulCollectorAttack ==
                SoulCollectorAttackKind.SoulBurst &&
            closeCollector.IsCombatAttackTelegraphing,
            "Soul Collector did not use its close-range defensive burst.");

        collector.ReceiveDamage(int.MaxValue);
        Require(!collector.HasActiveSoulField,
            "Soul Collector death left an orphaned Curse field.");
    }

    private static void ValidatePassFourIntroductions()
    {
        var knight = Create(EnemyType.CursedKnight, new Vector2(80f, 80f));
        var knightIntroductions = new EnemyIntroductionManager();
        knightIntroductions.Update(
            Frame(.05f), Vector2.Zero, new Enemy[] { knight }, null, false);
        Require(knightIntroductions.Active?.Name == "CURSED KNIGHT" &&
            knightIntroductions.Active.Title == "THE FORSAKEN OATH" &&
            knightIntroductions.Active.Description ==
                "HONOR DIED LONG AGO THE OATH DID NOT",
            "Cursed Knight first encounter copy changed.");

        var collector = Create(
            EnemyType.SoulCollector, new Vector2(90f, 80f));
        var collectorIntroductions = new EnemyIntroductionManager();
        collectorIntroductions.Update(
            Frame(.05f), Vector2.Zero,
            new Enemy[] { collector }, null, false);
        Require(collectorIntroductions.Active?.Name == "SOUL COLLECTOR" &&
            collectorIntroductions.Active.Title == "WARDEN OF LOST SOULS" &&
            collectorIntroductions.Active.Description ==
                "IT GATHERS THE DEAD SO NONE MAY EVER LEAVE",
            "Soul Collector first encounter copy changed.");
    }

    private static void ValidateDeathAndFallenKnightPolish()
    {
        Require(EnemyVisualProfile.DeathKnight.VisualSize ==
                new Vector2(142f, 126f) &&
            EnemyVisualProfile.FallenKnight.VisualSize ==
                new Vector2(136f, 134f) &&
            !EnemyVisualProfile.DeathKnight
                .GetAnimation(EnemyVisualState.Death).IsLooping &&
            !EnemyVisualProfile.FallenKnight
                .GetAnimation(EnemyVisualState.Death).IsLooping,
            "Final knight silhouettes or authored deaths changed.");

        DungeonMap map = new CatacombMapGenerator().Generate();
        DungeonRoom tomb = map.Rooms[8];
        float deathX = tomb.Bounds.Left + 720f;
        var deathKnight = Create(
            EnemyType.DeathKnight,
            new Vector2(deathX, tomb.GroundY - 39f),
            tomb.Id);
        deathKnight.PlaceAtAuthoredSocket(
            new Vector2(deathX, tomb.GroundY), false);
        var player = new PlayerCharacter(
            new Vector2(deathX + 115f, tomb.GroundY - 220f));
        var projectiles = new ProjectileManager();
        bool sawSweep = false;
        bool sawCrush = false;
        bool sawCharge = false;
        bool sawSlam = false;
        for (int frame = 0; frame < 900; frame++)
        {
            float offset = frame % 240 < 80 ? 190f : 115f;
            player.MoveTo(new Vector2(
                deathKnight.Position.X + offset,
                tomb.GroundY - 220f));
            Tick(deathKnight, player, map, projectiles, .05f);
            sawSweep |= deathKnight.DeathKnightAttack ==
                DeathKnightAttackKind.WarSweep;
            sawCrush |= deathKnight.DeathKnightAttack ==
                DeathKnightAttackKind.ExecutionCrush;
            sawCharge |= deathKnight.DeathKnightAttack ==
                DeathKnightAttackKind.DreadCharge;
            sawSlam |= deathKnight.DeathKnightAttack ==
                DeathKnightAttackKind.TombbreakerSlam;
        }
        Require(sawSweep && sawCrush && sawCharge && sawSlam,
            "Death Knight committed attack kit is incomplete.");
        float deathPoise = deathKnight.CurrentPoise;
        deathKnight.ReceiveDamage(8, 20f);
        Require(deathKnight.CurrentPoise > deathPoise - 12f,
            "Death Knight no longer resists light Poise pressure.");

        DungeonRoom hall = map.Rooms[9];
        float fallenX = hall.Bounds.Left + 820f;
        var fallen = Create(
            EnemyType.FallenKnight,
            new Vector2(fallenX, hall.GroundY - 43f),
            hall.Id);
        fallen.PlaceAtAuthoredSocket(
            new Vector2(fallenX, hall.GroundY), false);
        player.MoveTo(new Vector2(fallenX + 120f, hall.GroundY - 230f));
        fallen.ReceiveDamage(fallen.MaxHealth * 36 / 100);
        Tick(fallen, player, map, projectiles, .05f);
        Require(fallen.IsPhaseTransitioning && fallen.CombatPhase == 2,
            "Fallen Knight phase two lacks its safe visible transition.");
        Tick(fallen, player, map, projectiles,
            CatacombEnemy.FallenPhaseTwoTransitionSeconds + .05f);
        Require(fallen.ActiveFallenPhase == 2,
            "Fallen Knight phase two did not activate after its transition.");

        bool sawExtension = false;
        bool sawGraveStep = false;
        bool sawCursedCounter = false;
        for (int frame = 0; frame < 720; frame++)
        {
            float offset = frame % 180 < 70 ? 74f : 135f;
            player.MoveTo(new Vector2(
                fallen.Position.X + offset,
                hall.GroundY - 230f));
            Tick(fallen, player, map, projectiles, .05f);
            sawExtension |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.CursedExtensionSlash;
            sawGraveStep |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.GraveStep;
            sawCursedCounter |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.CursedCounter;
        }
        Require(sawExtension && sawGraveStep && sawCursedCounter,
            "Fallen Knight phase two control kit is incomplete.");

        fallen.ReceiveDamage(fallen.MaxHealth * 36 / 100);
        Tick(fallen, player, map, projectiles, .05f);
        Require(fallen.IsPhaseTransitioning && fallen.CombatPhase == 3,
            "Fallen Knight Final Oath lacks its kneeling transition.");
        Tick(fallen, player, map, projectiles,
            CatacombEnemy.FallenPhaseThreeTransitionSeconds + .05f);
        bool sawCleave = false;
        bool sawRush = false;
        bool sawJudgment = false;
        for (int frame = 0; frame < 900; frame++)
        {
            player.MoveTo(new Vector2(
                fallen.Position.X + 138f,
                hall.GroundY - 230f));
            Tick(fallen, player, map, projectiles, .05f);
            sawCleave |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.FinalOathCleave;
            sawRush |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.OathbreakerRush;
            sawJudgment |= fallen.FallenKnightAttack ==
                FallenKnightAttackKind.LastJudgment;
        }
        Require(sawCleave && sawRush && sawJudgment,
            "Fallen Knight Final Oath signature kit is incomplete.");

        var deathIntro = new EnemyIntroductionManager();
        deathIntro.Update(Frame(.05f), Vector2.Zero,
            new Enemy[] { Create(EnemyType.DeathKnight, Vector2.Zero) },
            null, false);
        Require(deathIntro.Active?.Title == "GUARDIAN OF THE WAR TOMB" &&
            deathIntro.Active.Description ==
                "DEATH ENDED THE BATTLE. IT DID NOT END HIS DUTY.",
            "Death Knight first encounter copy changed.");
        var fallenIntro = new EnemyIntroductionManager();
        fallenIntro.Update(Frame(.05f), Vector2.Zero,
            new Enemy[] { Create(EnemyType.FallenKnight, Vector2.Zero) },
            null, false);
        Require(fallenIntro.Active?.PresentationStrength == 3 &&
            fallenIntro.Active.Title == "THE LAST OATH OF THE CATACOMBS" &&
            fallenIntro.Active.Description ==
                "ONCE A GUARDIAN OF THE DEAD. NOW THEIR FINAL PRISONER.",
            "Fallen Knight boss introduction copy or strength changed.");
    }

    private static CatacombEnemy Create(
        EnemyType type,
        Vector2 position,
        int roomId = -1)
    {
        return new CatacombEnemy(type, position, 1, 1, roomId, false);
    }

    private static void Tick(
        CatacombEnemy enemy,
        PlayerCharacter player,
        DungeonMap map,
        ProjectileManager projectiles,
        float seconds)
    {
        float remaining = seconds;
        var roots = new RootHazardManager();
        while (remaining > 0f)
        {
            float step = MathF.Min(.05f, remaining);
            enemy.Update(Frame(step), player, map, projectiles, roots);
            remaining -= step;
        }
    }

    private static void Expire(
        EnemyIntroductionManager introductions,
        Enemy enemy)
    {
        for (int frame = 0; frame < 50; frame++)
        {
            introductions.Update(
                Frame(.05f),
                Vector2.Zero,
                new[] { enemy },
                null,
                false);
        }
    }

    private static GameTime Frame(float seconds) => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(seconds));

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(
                $"Catacomb enemy polish validation failed: {message}");
    }
}
