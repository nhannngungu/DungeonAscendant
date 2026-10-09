using System;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

/// <summary>
/// Headless contract checks for the Phase 1 Goblin-family presentation layer.
/// </summary>
public static class EnemyVisualValidation
{
    public static void ValidateOrThrow()
    {
        ValidateProfilesAndStateMapping();
        ValidatePhaseTwoProfilesAndStateMapping();
        ValidatePhaseThreeProfilesAndStateMapping();
        ValidatePhaseFourProfilesAndStateMapping();
        ValidateFinalPhaseProfilesAndStateMapping();
        ValidateHunterReleaseAlignment();
        ValidateSpiderWebReleaseAlignment();
        ValidateAmbushStateAlignment();
        ValidateHeavyEnemySkillAlignment();
        ValidateApexEnemySkillAlignment();
    }

    private static void ValidateProfilesAndStateMapping()
    {
        EnemyVisualProfile goblinProfile = EnemyVisualProfile.Goblin;
        EnemyVisualProfile hunterProfile = EnemyVisualProfile.GoblinHunter;
        Require(goblinProfile.VisualSize == new Vector2(52f, 50f),
            "Goblin visual dimensions");
        Require(hunterProfile.VisualSize == new Vector2(56f, 57f),
            "Hunter visual dimensions");
        Require(goblinProfile.VisualSize.Y <
            PlayerSpriteRenderer.VisualHeight,
            "Goblin remains smaller than Player");
        Require(hunterProfile.VisualSize.Y <
            PlayerSpriteRenderer.VisualHeight,
            "Hunter remains smaller than Player");
        Require(!goblinProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "Goblin death is one-shot");
        Require(!hunterProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "Hunter death is one-shot");

        var goblin = new Goblin(Vector2.Zero);
        Require(goblin.Size == new Vector2(36f, 44f),
            "Goblin collision size unchanged");
        Require(goblin.Attack.WindupSeconds == 0.48f &&
            goblin.Attack.ActiveSeconds == 0.17f &&
            goblin.Attack.RecoverySeconds == 0.40f,
            "Goblin combat timings unchanged");
        var controller = new EnemyAnimationController(goblinProfile);
        controller.Update(Frame(1f / 60f), goblin);
        Require(controller.State == EnemyVisualState.Idle,
            "Goblin idle mapping");
        goblin.Attack.StartIfReady();
        controller.Update(Frame(1f / 60f), goblin);
        Require(controller.State == EnemyVisualState.AttackWindup,
            "Goblin windup mapping");
        goblin.Attack.Update(Frame(0.49f));
        controller.Update(Frame(1f / 60f), goblin);
        Require(controller.State == EnemyVisualState.Attack,
            "Goblin active mapping");
        goblin.Attack.Update(Frame(0.17f));
        controller.Update(Frame(1f / 60f), goblin);
        Require(controller.State == EnemyVisualState.AttackRecovery,
            "Goblin recovery mapping");

        var hurtGoblin = new Goblin(Vector2.Zero);
        hurtGoblin.ReceiveDamage(1);
        var hurtController = new EnemyAnimationController(goblinProfile);
        hurtController.Update(Frame(1f / 60f), hurtGoblin);
        Require(hurtController.State == EnemyVisualState.Hurt,
            "Goblin hurt mapping");

        var staggeredGoblin = new Goblin(Vector2.Zero);
        staggeredGoblin.ApplyPoiseDamage(staggeredGoblin.MaxPoise);
        var staggerController = new EnemyAnimationController(goblinProfile);
        staggerController.Update(Frame(1f / 60f), staggeredGoblin);
        Require(staggerController.State == EnemyVisualState.Stagger,
            "Goblin stagger mapping");

        var deadGoblin = new Goblin(Vector2.Zero);
        deadGoblin.ReceiveDamage(int.MaxValue);
        var deathController = new EnemyAnimationController(goblinProfile);
        deathController.Update(Frame(1f / 60f), deadGoblin);
        Require(deathController.State == EnemyVisualState.Death,
            "Goblin death mapping");
        Require(!deadGoblin.IsDeathPresentationComplete,
            "death presentation begins before cleanup");
        deadGoblin.UpdateDeathPresentation(Frame(0.6f));
        Require(deadGoblin.IsDeathPresentationComplete,
            "death presentation completes once");

        var hunter = new GoblinHunter(Vector2.Zero, 1, false, -1, 1);
        Require(hunter.Size == new Vector2(36f, 46f),
            "Hunter collision size unchanged");
        Require(hunter.Attack.WindupSeconds == 0.26f &&
            hunter.Attack.ActiveSeconds == 0.06f &&
            hunter.Attack.RecoverySeconds == 0.18f,
            "Hunter combat timings unchanged");
        hunter.Attack.StartIfReady();
        var hunterController = new EnemyAnimationController(hunterProfile);
        hunterController.Update(Frame(1f / 60f), hunter);
        Require(hunterController.State == EnemyVisualState.Aim,
            "Hunter aim mapping");
        hunter.Attack.Update(Frame(0.27f));
        hunterController.Update(Frame(1f / 60f), hunter);
        Require(hunterController.State == EnemyVisualState.Shoot,
            "Hunter shoot mapping");

        var staggeredHunter = new GoblinHunter(
            Vector2.Zero, 1, false, -1, 1);
        staggeredHunter.ApplyPoiseDamage(staggeredHunter.MaxPoise);
        var hunterStaggerController = new EnemyAnimationController(
            hunterProfile);
        hunterStaggerController.Update(
            Frame(1f / 60f),
            staggeredHunter);
        Require(hunterStaggerController.State == EnemyVisualState.Stagger,
            "Hunter stagger mapping");
    }

    private static void ValidateHunterReleaseAlignment()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            var session = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 173);
            Press(session, Keys.Enter);
            Enemy goblin = session.WildForestShowcaseEnemies[0];
            Require(goblin?.Type == EnemyType.Goblin,
                "showcase visual test begins with Goblin");
            goblin.ReceiveDamage(int.MaxValue);
            Tick(session, 0.5f);
            Require(session.Enemies.DefeatPresentations.Count == 1,
                "Goblin collapse retained after combat removal");
            Tick(session, 0.5f);
            Require(session.Enemies.DefeatPresentations.Count == 0,
                "Goblin collapse cleaned up once");
            var hunter = session.WildForestShowcaseEnemies[1] as GoblinHunter;
            Require(hunter != null,
                "showcase includes Goblin Hunter");

            session.Player.MoveTo(
                hunter.Position + new Vector2(90f, 0f));
            Tick(session, 1f / 60f);
            Require(hunter.IsRetreating &&
                hunter.Facing == EnemyFacingDirection.Left &&
                hunter.VisualVelocityX < 0f,
                "Hunter retreat faces its movement direction");

            session.Player.MoveTo(
                hunter.Position + new Vector2(-190f, -70f));
            Tick(session, 1f / 60f);
            Require(hunter.Attack.IsTelegraphing,
                "Hunter visibly aims before release");
            Require(hunter.VisualAimDirection.X < 0f &&
                hunter.VisualAimDirection.Y < 0f,
                "Hunter bow reflects elevated left target");
            Tick(session, 0.27f);
            Require(session.Projectiles.Projectiles.Count == 1,
                "Hunter releases one arrow on active phase");
            Projectile arrow = session.Projectiles.Projectiles[0];
            Require(arrow.Type == ProjectileType.Arrow && arrow.Blockable,
                "Hunter arrow remains shield-blockable");
            Require(Vector2.Distance(
                    arrow.SourcePosition,
                    hunter.GetArrowReleasePosition()) < 0.1f,
                "arrow source matches visual bow release");
            Require(arrow.Velocity.X < 0f && arrow.Velocity.Y < 0f,
                "arrow preserves vertical aim");
            Require(GameSession.DebugWildForestShowcase,
                "showcase debug spawn remains enabled");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static void ValidatePhaseTwoProfilesAndStateMapping()
    {
        EnemyVisualProfile wolfProfile = EnemyVisualProfile.DireWolf;
        EnemyVisualProfile spiderProfile = EnemyVisualProfile.GiantSpider;
        Require(wolfProfile.VisualSize == new Vector2(100f, 44f),
            "Dire Wolf visual dimensions");
        Require(spiderProfile.VisualSize == new Vector2(100f, 36f),
            "Giant Spider visual dimensions");
        Require(wolfProfile.VisualSize.Y / PlayerSpriteRenderer.VisualHeight >=
                0.55f &&
            wolfProfile.VisualSize.Y / PlayerSpriteRenderer.VisualHeight <=
                0.65f,
            "Dire Wolf height remains waist-to-chest scale");
        Require(spiderProfile.VisualSize.Y / PlayerSpriteRenderer.VisualHeight >=
                0.45f &&
            spiderProfile.VisualSize.Y / PlayerSpriteRenderer.VisualHeight <=
                0.55f,
            "Giant Spider remains low relative to Player");
        Require(wolfProfile.VisualSize.X ==
                PlayerSpriteRenderer.ConceptualFrameWidth &&
            spiderProfile.VisualSize.X ==
                PlayerSpriteRenderer.ConceptualFrameWidth,
            "creature envelopes match one Player conceptual frame width");
        Require(CorruptedBeastSpriteRenderer.DireWolfVisualScale ==
                new Vector2(0.76f, 0.69f) &&
            CorruptedBeastSpriteRenderer.GiantSpiderVisualScale ==
                new Vector2(0.69f, 0.88f),
            "visual scale correction remains presentation-only");
        Require(!wolfProfile.GetAnimation(EnemyVisualState.Death).IsLooping &&
            !spiderProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "creature deaths are one-shot");

        var wolf = new DireWolf(Vector2.Zero, 1, false, -1, 1);
        Require(wolf.Size == new Vector2(52f, 32f),
            "Dire Wolf hurtbox unchanged");
        Require(wolf.Attack.WindupSeconds == 0.24f &&
            wolf.Attack.ActiveSeconds == 0.10f &&
            wolf.Attack.RecoverySeconds == 0.20f,
            "Dire Wolf readable lunge timing");
        var wolfController = new EnemyAnimationController(wolfProfile);
        wolf.Attack.StartIfReady();
        wolfController.Update(Frame(1f / 60f), wolf);
        Require(wolfController.State == EnemyVisualState.AttackWindup,
            "Dire Wolf crouch maps to windup");
        wolf.Attack.Update(Frame(0.25f));
        wolfController.Update(Frame(1f / 60f), wolf);
        Require(wolfController.State == EnemyVisualState.LungeAttack &&
            wolf.Attack.IsActive,
            "Dire Wolf visual lunge maps to active hit phase");

        var retreatingWolf = new DireWolf(Vector2.Zero, 1, false, -1, 1);
        retreatingWolf.ReceiveDamage(retreatingWolf.MaxHealth - 1);
        retreatingWolf.UpdateTimers(Frame(0.15f));
        var retreatController = new EnemyAnimationController(wolfProfile);
        retreatController.Update(Frame(1f / 60f), retreatingWolf);
        Require(retreatingWolf.IsRetreating &&
            retreatController.State == EnemyVisualState.Retreat,
            "Dire Wolf retreat has a distinct visual state");

        var spider = new GiantSpider(Vector2.Zero, 1, false, -1, 1);
        Require(spider.Size == new Vector2(50f, 36f),
            "Giant Spider core hurtbox unchanged");
        Require(spider.Attack.WindupSeconds == 0.26f &&
            spider.Attack.ActiveSeconds == 0.10f &&
            spider.Attack.RecoverySeconds == 0.24f,
            "Giant Spider readable bite timing");
        spider.Attack.StartIfReady();
        var spiderController = new EnemyAnimationController(spiderProfile);
        spiderController.Update(Frame(1f / 60f), spider);
        Require(spiderController.State == EnemyVisualState.AttackWindup,
            "Giant Spider front rise maps to bite windup");
        spider.Attack.Update(Frame(0.27f));
        spiderController.Update(Frame(1f / 60f), spider);
        Require(spiderController.State == EnemyVisualState.BiteAttack &&
            spider.Attack.IsActive,
            "Giant Spider fang snap maps to active hit phase");

        SpriteAnimation run = wolfProfile.GetAnimation(EnemyVisualState.Run);
        Require(run.GetFrame(0.07f) > run.GetFrame(0f),
            "run frames progress forward independent of facing");
    }

    private static void ValidateSpiderWebReleaseAlignment()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            var session = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 291);
            Press(session, Keys.Enter);
            var spider = session.WildForestShowcaseEnemies[3] as GiantSpider;
            Require(spider != null, "showcase includes Giant Spider");
            session.Player.MoveTo(spider.Position + new Vector2(-240f, 0f));
            Tick(session, 1.11f);
            Require(spider.WebState == GiantSpiderWebState.Prepare,
                "web attack enters visible prepare state");
            var controller = new EnemyAnimationController(
                EnemyVisualProfile.GiantSpider);
            controller.Update(Frame(1f / 60f), spider);
            Require(controller.State == EnemyVisualState.WebPrepare,
                "web prepare visual mapping");

            Tick(session, GiantSpider.WebPrepareSeconds + 0.01f);
            Require(spider.WebState == GiantSpiderWebState.Shoot &&
                session.Projectiles.Projectiles.Count == 1,
                "web releases after prepare completes");
            Projectile web = session.Projectiles.Projectiles[0];
            Require(web.Type == ProjectileType.WebShot &&
                Vector2.Distance(web.SourcePosition,
                    spider.GetWebReleasePosition()) < 0.1f,
                "web projectile source matches rendered fang origin");
            Require(web.SlowDurationSeconds ==
                    ProjectileManager.WebShotSlowDurationSeconds &&
                web.SlowMovementMultiplier ==
                    ProjectileManager.WebSlowMultiplier,
                "web Slow mechanics preserved");

            controller.Update(Frame(1f / 60f), spider);
            Require(controller.State == EnemyVisualState.WebShoot,
                "web release visual mapping");
            Tick(session, GiantSpider.WebShootSeconds + 0.01f);
            controller.Update(Frame(1f / 60f), spider);
            Require(controller.State == EnemyVisualState.WebRecovery,
                "web recovery visual mapping");
            Enemy wolf = session.WildForestShowcaseEnemies[2];
            wolf.ReceiveDamage(int.MaxValue);
            spider.ReceiveDamage(int.MaxValue);
            Tick(session, 0.1f);
            Require(session.Enemies.DefeatPresentations.Count >= 2,
                "Wolf and Spider deaths remain visible for collapse");
            Tick(session, 0.6f);
            Require(session.Enemies.DefeatPresentations.Count == 0,
                "Wolf and Spider death presentations clean up once");
            Require(GameSession.DebugWildForestShowcase,
                "showcase remains enabled during phase two validation");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static void ValidatePhaseThreeProfilesAndStateMapping()
    {
        EnemyVisualProfile batProfile = EnemyVisualProfile.BloodBat;
        EnemyVisualProfile crawlerProfile = EnemyVisualProfile.ThornCrawler;
        Require(batProfile.VisualSize == new Vector2(92f, 42f),
            "Blood Bat visual dimensions");
        Require(crawlerProfile.VisualSize == new Vector2(68f, 30f),
            "Thorn Crawler visual dimensions");
        Require(batProfile.VisualSize.X /
                PlayerSpriteRenderer.ConceptualFrameWidth >= 0.8f &&
            batProfile.VisualSize.X /
                PlayerSpriteRenderer.ConceptualFrameWidth <= 1f,
            "Blood Bat wingspan remains below boss scale");
        Require(crawlerProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight >= 0.35f &&
            crawlerProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight <= 0.45f &&
            crawlerProfile.VisualSize.X <
                EnemyVisualProfile.DireWolf.VisualSize.X,
            "Thorn Crawler stays low and shorter than Dire Wolf");
        Require(!batProfile.GetAnimation(EnemyVisualState.Death).IsLooping &&
            !crawlerProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "phase three deaths are one-shot");

        var bat = new BloodBat(Vector2.Zero, 1, false, -1, 1);
        Require(bat.Size == new Vector2(38f, 24f),
            "Blood Bat body hurtbox unchanged");
        Require(bat.MeleeTargetBounds.Width == 16 &&
            bat.MeleeTargetBounds.Height == 12,
            "high-altitude Bat target area excludes decorative wings");
        Require(bat.Attack.WindupSeconds == 0.11f &&
            bat.Attack.ActiveSeconds == 0.07f &&
            bat.Attack.RecoverySeconds == 0.15f,
            "Blood Bat combat timings unchanged");
        var batController = new EnemyAnimationController(batProfile);
        batController.Update(Frame(1f / 60f), bat);
        Require(batController.State == EnemyVisualState.IdleHover,
            "Blood Bat idle hover mapping");
        bat.Attack.StartIfReady();
        batController.Update(Frame(1f / 60f), bat);
        Require(batController.State == EnemyVisualState.DiveWindup,
            "Blood Bat windup mapping");
        bat.Attack.Update(Frame(0.12f));
        batController.Update(Frame(1f / 60f), bat);
        Require(batController.State == EnemyVisualState.DiveAttack &&
            bat.Attack.IsActive,
            "Blood Bat dive contact maps to active phase");
        bat.Attack.Update(Frame(0.08f));
        batController.Update(Frame(1f / 60f), bat);
        Require(batController.State == EnemyVisualState.DiveRecovery,
            "Blood Bat recovery mapping");

        var crawler = new ThornCrawler(Vector2.Zero, 1, false, -1, 1);
        Require(crawler.Size == new Vector2(42f, 44f),
            "Thorn Crawler physics size unchanged");
        Require(!crawler.CanBeTargeted,
            "hidden Thorn Crawler remains untargetable");
        Require(crawler.Attack.WindupSeconds == 0.22f &&
            crawler.Attack.ActiveSeconds == 0.09f &&
            crawler.Attack.RecoverySeconds == 0.24f,
            "Thorn Crawler combat timings unchanged");
        var crawlerController = new EnemyAnimationController(crawlerProfile);
        crawlerController.Update(Frame(1f / 60f), crawler);
        Require(crawlerController.State == EnemyVisualState.Hidden,
            "Thorn Crawler hidden mapping");

        var hurtBat = new BloodBat(Vector2.Zero, 1, false, -1, 1);
        hurtBat.ReceiveDamage(1);
        var hurtBatController = new EnemyAnimationController(batProfile);
        hurtBatController.Update(Frame(1f / 60f), hurtBat);
        Require(hurtBatController.State == EnemyVisualState.Hurt,
            "Blood Bat hurt mapping");

    }

    private static void ValidateAmbushStateAlignment()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            var session = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 463);
            Press(session, Keys.Enter);
            var bat = session.WildForestShowcaseEnemies[4] as BloodBat;
            var crawler = session.WildForestShowcaseEnemies[5] as ThornCrawler;
            Require(bat != null && crawler != null,
                "showcase includes Blood Bat and Thorn Crawler in order");

            session.Player.MoveTo(bat.Position + new Vector2(-200f, 0f));
            Tick(session, 1f / 60f);
            var batController = new EnemyAnimationController(
                EnemyVisualProfile.BloodBat);
            batController.Update(Frame(1f / 60f), bat);
            Require(batController.State == EnemyVisualState.Fly &&
                bat.Facing == EnemyFacingDirection.Left,
                "Blood Bat wing beat and left-facing movement mapping");
            Tick(session, 1.1f);
            batController.Update(Frame(1f / 60f), bat);
            Require(bat.State == BloodBatState.Diving &&
                batController.State == EnemyVisualState.Approach,
                "Blood Bat enters visual approach before low altitude");
            session.Player.MoveTo(bat.Position + new Vector2(-240f, 0f));
            Tick(session, 0.35f);
            batController.Update(Frame(1f / 60f), bat);
            Require(bat.State == BloodBatState.Diving &&
                batController.State == EnemyVisualState.LowAltitude &&
                bat.IsLowAltitude &&
                bat.MeleeTargetBounds == bat.Bounds,
                "Blood Bat low-altitude vulnerability is visually mapped");

            bat.ReceiveDamage(int.MaxValue);
            Tick(session, 0.1f);
            Require(session.Enemies.DefeatPresentations.Count == 1 &&
                session.Enemies.DefeatPresentations[0] == bat &&
                !float.IsNaN(bat.DeathLandingY),
                "Blood Bat fall persists toward a known ground landing");
            Tick(session, 0.6f);
            Require(session.Enemies.DefeatPresentations.Count == 0,
                "Blood Bat death presentation cleans up once");

            session.Player.MoveTo(crawler.Position + new Vector2(-70f, 0f));
            Tick(session, 1f / 60f);
            Tick(session, 1f / 60f);
            var crawlerController = new EnemyAnimationController(
                EnemyVisualProfile.ThornCrawler);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawler.State == ThornCrawlerState.Warning &&
                crawlerController.State == EnemyVisualState.Warning &&
                !crawler.CanBeTargeted,
                "Thorn Crawler warning remains underground and untargetable");
            Tick(session, ThornCrawler.WarningDurationSeconds + 0.01f);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawler.State == ThornCrawlerState.Emerging &&
                crawlerController.State == EnemyVisualState.Emerging &&
                crawler.CanBeTargeted,
                "Thorn Crawler emerges before it can attack");
            crawler.ReceiveDamage(1);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawlerController.State == EnemyVisualState.Hurt,
                "exposed Thorn Crawler hurt mapping");
            crawler.ApplyPoiseDamage(crawler.MaxPoise);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawlerController.State == EnemyVisualState.Stagger,
                "exposed Thorn Crawler stagger mapping");
            Tick(session, 0.69f);
            session.Player.MoveTo(crawler.Position + new Vector2(-30f, 0f));
            Tick(session, 0.37f);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawler.State == ThornCrawlerState.Attacking &&
                crawlerController.State == EnemyVisualState.AttackWindup &&
                crawler.Attack.IsTelegraphing,
                $"Thorn Crawler visible windup precedes damage " +
                $"({crawler.State}/{crawlerController.State}/" +
                $"{crawler.Attack.Phase}/stagger={crawler.IsStaggered})");
            Tick(session, 0.23f);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawlerController.State == EnemyVisualState.Attack &&
                crawler.Attack.IsActive,
                "Thorn Crawler vine extension matches active hit phase");
            Tick(session, 1.12f);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawler.State == ThornCrawlerState.Recovering &&
                crawlerController.State == EnemyVisualState.Recovery,
                "Thorn Crawler has a distinct post-attack recovery");
            Tick(session, 0.91f);
            crawlerController.Update(Frame(1f / 60f), crawler);
            Require(crawler.State == ThornCrawlerState.Burrowing &&
                crawlerController.State == EnemyVisualState.Burrow &&
                !crawler.CanBeTargeted,
                "Thorn Crawler burrow retracts before hidden state");

            var deathSession = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 487);
            Press(deathSession, Keys.Enter);
            var deathCrawler = deathSession.WildForestShowcaseEnemies[5]
                as ThornCrawler;
            Require(deathCrawler != null,
                "showcase provides exposed Crawler death fixture");
            deathSession.Player.MoveTo(
                deathCrawler.Position + new Vector2(-70f, 0f));
            Tick(deathSession, 1f / 60f);
            Tick(deathSession, 1f / 60f);
            Tick(deathSession, ThornCrawler.WarningDurationSeconds + 0.01f);
            Require(deathCrawler.CanBeTargeted,
                "Thorn Crawler death fixture is exposed");
            deathCrawler.ReceiveDamage(int.MaxValue);
            Tick(deathSession, 0.1f);
            Require(deathSession.Enemies.DefeatPresentations.Count == 1 &&
                deathSession.Enemies.DefeatPresentations[0] == deathCrawler,
                "Thorn Crawler death remains visible as dead root mass");
            Tick(deathSession, 0.6f);
            Require(deathSession.Enemies.DefeatPresentations.Count == 0,
                "Thorn Crawler death presentation cleans up once");
            Require(GameSession.DebugWildForestShowcase,
                "showcase remains enabled during phase three validation");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static void ValidatePhaseFourProfilesAndStateMapping()
    {
        EnemyVisualProfile treantProfile = EnemyVisualProfile.CorruptedTreant;
        EnemyVisualProfile chiefProfile = EnemyVisualProfile.GoblinChief;
        Require(treantProfile.VisualSize == new Vector2(108f, 106f),
            "Corrupted Treant visual dimensions");
        Require(chiefProfile.VisualSize == new Vector2(78f, 71f),
            "Goblin Chief visual dimensions");
        Require(treantProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight >= 1.4f &&
            treantProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight <= 1.6f,
            "Corrupted Treant remains heavy normal scale");
        Require(chiefProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight >= 0.9f &&
            chiefProfile.VisualSize.Y /
                PlayerSpriteRenderer.VisualHeight <= 1.05f &&
            chiefProfile.VisualSize.Y >
                EnemyVisualProfile.Goblin.VisualSize.Y,
            "Goblin Chief reads as Player-scale elite");
        Require(!treantProfile.GetAnimation(EnemyVisualState.Death).IsLooping &&
            !chiefProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "phase four deaths are one-shot");

        var treant = new CorruptedTreant(Vector2.Zero, 1, false, -1, 1);
        Require(treant.Size == new Vector2(64f, 78f),
            "Treant hurtbox excludes decorative branches");
        Require(treant.Attack.WindupSeconds == 0.38f &&
            treant.Attack.ActiveSeconds == 0.13f &&
            treant.Attack.RecoverySeconds == 0.38f,
            "Treant heavy melee timing unchanged");
        var treantController = new EnemyAnimationController(treantProfile);
        treant.Attack.StartIfReady();
        treantController.Update(Frame(1f / 60f), treant);
        Require(treantController.State == EnemyVisualState.AttackWindup,
            "Treant branch backswing maps to windup");
        treant.Attack.Update(Frame(0.39f));
        treantController.Update(Frame(1f / 60f), treant);
        Require(treantController.State == EnemyVisualState.HeavyMeleeAttack &&
            treant.Attack.IsActive,
            "Treant branch strike maps to active phase");
        treant.Attack.Update(Frame(0.14f));
        treantController.Update(Frame(1f / 60f), treant);
        Require(treantController.State == EnemyVisualState.AttackRecovery,
            "Treant dragging arm maps to recovery");

        var chief = new GoblinChief(Vector2.Zero, 1, -1, 1);
        Require(chief.Size == new Vector2(48f, 58f),
            "Chief hurtbox excludes horns, weapon, and banner");
        Require(chief.Attack.WindupSeconds == 0.24f &&
            chief.Attack.ActiveSeconds == 0.10f &&
            chief.Attack.RecoverySeconds == 0.26f,
            "Chief melee timing unchanged");
        var chiefController = new EnemyAnimationController(chiefProfile);
        chief.Attack.StartIfReady();
        chiefController.Update(Frame(1f / 60f), chief);
        Require(chiefController.State == EnemyVisualState.AttackWindup,
            "Chief axe raise maps to windup");
        chief.Attack.Update(Frame(0.25f));
        chiefController.Update(Frame(1f / 60f), chief);
        Require(chiefController.State == EnemyVisualState.Attack &&
            chief.Attack.IsActive,
            "Chief axe swing maps to active phase");

        var hurtTreant = new CorruptedTreant(Vector2.Zero, 1, false, -1, 1);
        hurtTreant.ReceiveDamage(1);
        var hurtTreantController = new EnemyAnimationController(treantProfile);
        hurtTreantController.Update(Frame(1f / 60f), hurtTreant);
        Require(hurtTreantController.State == EnemyVisualState.Hurt,
            "Treant restrained hurt mapping");

        var staggeredChief = new GoblinChief(Vector2.Zero, 1, -1, 1);
        staggeredChief.ApplyPoiseDamage(staggeredChief.MaxPoise);
        var staggeredChiefController = new EnemyAnimationController(
            chiefProfile);
        staggeredChiefController.Update(Frame(1f / 60f), staggeredChief);
        Require(staggeredChiefController.State == EnemyVisualState.Stagger,
            "Chief heavy stagger mapping");
    }

    private static void ValidateHeavyEnemySkillAlignment()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            var session = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 613);
            Press(session, Keys.Enter);
            var treant = session.WildForestShowcaseEnemies[6]
                as CorruptedTreant;
            Require(treant != null,
                "showcase includes Corrupted Treant in order");

            for (int frame = 0;
                 frame < 150 && session.RootHazards.Hazards.Count == 0;
                 frame++)
            {
                session.Player.MoveTo(
                    treant.Position + new Vector2(-180f, 0f));
                Tick(session, 1f / 60f);
            }

            Require(session.RootHazards.Hazards.Count == 1,
                "Treant creates one telegraphed Root Strike");
            RootHazard root = session.RootHazards.Hazards[0];
            var treantController = new EnemyAnimationController(
                EnemyVisualProfile.CorruptedTreant);
            treantController.Update(Frame(1f / 60f), treant);
            Require(root.Owner == treant &&
                root.State == RootHazardState.Telegraph &&
                root.TelegraphDurationSeconds ==
                    CorruptedTreant.RootTelegraphSeconds &&
                treantController.State == EnemyVisualState.RootStrikeWindup,
                "Root ground warning and Treant windup share the same clock");

            session.Player.MoveTo(
                treant.Position + new Vector2(-180f, 0f));
            Tick(session, CorruptedTreant.RootTelegraphSeconds + 0.01f);
            treantController.Update(Frame(1f / 60f), treant);
            Require(root.State == RootHazardState.Active &&
                treantController.State == EnemyVisualState.RootStrike,
                "Root eruption aligns with active hazard transition");
            session.Player.MoveTo(
                treant.Position + new Vector2(-180f, 0f));
            Tick(session, CorruptedTreant.RootStrikeVisualSeconds + 0.01f);
            treantController.Update(Frame(1f / 60f), treant);
            Require(treantController.State ==
                    EnemyVisualState.RootStrikeRecovery,
                "Treant has distinct Root Strike recovery");

            var chiefSession = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 641);
            Press(chiefSession, Keys.Enter);
            var chief = chiefSession.WildForestShowcaseEnemies[7]
                as GoblinChief;
            Require(chief != null,
                "showcase includes Goblin Chief in order");
            Enemy ally = chiefSession.Enemies.SpawnWildForestShowcaseEnemy(
                EnemyType.Goblin,
                chiefSession.CurrentDungeon.StartRoom,
                chief.Position.X + 100f,
                chiefSession.Player.Level,
                chiefSession.WorldTier);
            chiefSession.Player.MoveTo(
                chief.Position + new Vector2(-180f, 0f));
            Tick(chiefSession, 2.51f);
            var chiefController = new EnemyAnimationController(
                EnemyVisualProfile.GoblinChief);
            chiefController.Update(Frame(1f / 60f), chief);
            Require(chief.WarCryState == GoblinChiefWarCryState.Windup &&
                chiefController.State == EnemyVisualState.WarCryWindup &&
                !ally.IsBuffed,
                "War Cry plants stance before applying buff");
            Tick(chiefSession, GoblinChief.WarCryWindupSeconds + 0.01f);
            chiefController.Update(Frame(1f / 60f), chief);
            Require(chief.WarCryState == GoblinChiefWarCryState.Cry &&
                chiefController.State == EnemyVisualState.WarCry &&
                ally.IsBuffed,
                "War Cry pulse aligns with Goblin-family buff application");
            Tick(chiefSession, GoblinChief.WarCryActiveSeconds + 0.01f);
            chiefController.Update(Frame(1f / 60f), chief);
            Require(chief.WarCryState == GoblinChiefWarCryState.Recovery &&
                chiefController.State == EnemyVisualState.WarCryRecovery,
                "War Cry has a distinct recovery pose");

            var deathSession = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 659);
            Press(deathSession, Keys.Enter);
            Enemy deathTreant = deathSession.WildForestShowcaseEnemies[6];
            Enemy deathChief = deathSession.WildForestShowcaseEnemies[7];
            deathTreant.ReceiveDamage(int.MaxValue);
            deathChief.ReceiveDamage(int.MaxValue);
            Tick(deathSession, 0.1f);
            Require(deathSession.Enemies.DefeatPresentations.Count == 2,
                "Treant and Chief deaths remain visible after removal");
            Tick(deathSession, 0.6f);
            Require(deathSession.Enemies.DefeatPresentations.Count == 0,
                "Treant and Chief death presentations clean up once");
            Require(GameSession.DebugWildForestShowcase,
                "showcase remains enabled during phase four validation");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static void ValidateFinalPhaseProfilesAndStateMapping()
    {
        EnemyVisualProfile motherProfile = EnemyVisualProfile.MotherSpider;
        EnemyVisualProfile spiderlingProfile = EnemyVisualProfile.Spiderling;
        Require(motherProfile.VisualSize == new Vector2(140f, 52f),
            "Mother Spider visual dimensions");
        Require(spiderlingProfile.VisualSize == new Vector2(36f, 18f),
            "Spiderling visual dimensions");
        Require(motherProfile.VisualSize.X /
                EnemyVisualProfile.GiantSpider.VisualSize.X >= 1.3f &&
            motherProfile.VisualSize.X /
                EnemyVisualProfile.GiantSpider.VisualSize.X <= 1.5f &&
            motherProfile.VisualSize.Y /
                EnemyVisualProfile.GiantSpider.VisualSize.Y >= 1.3f &&
            motherProfile.VisualSize.Y /
                EnemyVisualProfile.GiantSpider.VisualSize.Y <= 1.5f,
            "Mother Spider remains elite rather than boss scale");
        Require(WildForestApexSpriteRenderer.AncientTreantVisualSize ==
                new Vector2(190f, 168f) &&
            WildForestApexSpriteRenderer.AncientTreantVisualSize.Y /
                PlayerSpriteRenderer.VisualHeight >= 2.2f &&
            WildForestApexSpriteRenderer.AncientTreantVisualSize.Y /
                PlayerSpriteRenderer.VisualHeight <= 2.6f,
            "Ancient Treant is unmistakably boss scale");
        Require(WildForestApexSpriteRenderer.AncientTreantVisualSize.Y >
                EnemyVisualProfile.CorruptedTreant.VisualSize.Y * 1.5f,
            "Ancient Treant clearly exceeds Corrupted Treant");
        Require(!motherProfile.GetAnimation(EnemyVisualState.Death).IsLooping,
            "Mother Spider death is one-shot");

        var mother = new MotherSpider(Vector2.Zero, 1, -1, 1);
        Require(mother.Size == new Vector2(72f, 54f),
            "Mother Spider hurtbox excludes long legs");
        Require(mother.Attack.WindupSeconds == 0.28f &&
            mother.Attack.ActiveSeconds == 0.11f &&
            mother.Attack.RecoverySeconds == 0.30f,
            "Mother Spider bite timings unchanged");
        var motherController = new EnemyAnimationController(motherProfile);
        mother.Attack.StartIfReady();
        motherController.Update(Frame(1f / 60f), mother);
        Require(motherController.State == EnemyVisualState.AttackWindup,
            "Mother Spider front brace maps to bite windup");
        mother.Attack.Update(Frame(0.29f));
        motherController.Update(Frame(1f / 60f), mother);
        Require(motherController.State == EnemyVisualState.BiteAttack &&
            mother.Attack.IsActive,
            "Mother Spider fang snap maps to active bite");
        mother.Attack.Update(Frame(0.12f));
        motherController.Update(Frame(1f / 60f), mother);
        Require(motherController.State == EnemyVisualState.AttackRecovery,
            "Mother Spider heavy return maps to recovery");

        var boss = new AncientTreant(Vector2.Zero, -1, 1, 1, 1);
        Require(boss.Size == new Vector2(110f, 126f),
            "boss hurtbox excludes crown branches and giant roots");
        Require(boss.Attack.WindupSeconds == 0.44f &&
            boss.Attack.ActiveSeconds == 0.14f &&
            boss.Attack.RecoverySeconds == 0.42f,
            "boss heavy melee timings unchanged");
        Require(boss.AttackArea.Right - boss.Bounds.Right ==
                (int)boss.Attack.Range,
            "boss branch attack area retains configured reach");
        boss.ApplyPoiseDamage(boss.MaxPoise);
        Require(boss.IsStaggered,
            "boss high-poise stagger remains available as punish window");
    }

    private static void ValidateApexEnemySkillAlignment()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            var session = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 719);
            Press(session, Keys.Enter);
            var mother = session.WildForestShowcaseEnemies[8]
                as MotherSpider;
            Require(mother != null,
                "showcase includes Mother Spider in order");
            session.Player.MoveTo(mother.Position + new Vector2(-200f, 0f));
            Tick(session, 0.91f);
            var motherController = new EnemyAnimationController(
                EnemyVisualProfile.MotherSpider);
            motherController.Update(Frame(1f / 60f), mother);
            Require(mother.SpecialState ==
                    MotherSpiderSpecialState.WebPrepare &&
                motherController.State == EnemyVisualState.WebPrepare,
                "Mother Spider web attack has visible preparation");
            Tick(session, MotherSpider.WebPrepareSeconds + 0.01f);
            motherController.Update(Frame(1f / 60f), mother);
            Require(mother.SpecialState == MotherSpiderSpecialState.WebShoot &&
                motherController.State == EnemyVisualState.WebShoot &&
                session.Projectiles.Projectiles.Count == 1,
                "Mother Spider releases web on shoot frame");
            Projectile eliteWeb = session.Projectiles.Projectiles[0];
            Require(eliteWeb.EmpoweredVisual &&
                Vector2.Distance(eliteWeb.SourcePosition,
                    mother.GetWebReleasePosition()) < 0.1f,
                "elite web visual originates at rendered fangs");
            Tick(session, MotherSpider.WebShootSeconds + 0.01f);
            motherController.Update(Frame(1f / 60f), mother);
            Require(motherController.State == EnemyVisualState.WebRecovery,
                "Mother Spider web attack has heavy recovery");

            Tick(session, 1.7f);
            motherController.Update(Frame(1f / 60f), mother);
            Require(mother.SpecialState ==
                    MotherSpiderSpecialState.SummonWindup &&
                motherController.State == EnemyVisualState.SummonWindup,
                "egg sacs contract before Spiderling summon");
            Tick(session, MotherSpider.SummonWindupSeconds + 0.01f);
            motherController.Update(Frame(1f / 60f), mother);
            Require(mother.SpecialState == MotherSpiderSpecialState.Summon &&
                motherController.State == EnemyVisualState.SummonSpiderlings,
                "Spiderling appears during egg-sac opening frame");

            int spiderlingCount = 0;
            foreach (Enemy enemy in session.Enemies.Enemies)
            {
                if (enemy is Spiderling child &&
                    ReferenceEquals(child.Mother, mother))
                {
                    spiderlingCount++;
                    Require(Vector2.Distance(
                            child.Position,
                            mother.Position) <=
                        EnemyManager.WildForestShowcaseSummonRadius + 1f,
                        "summoned Spiderling stays local to Mother");
                }
            }
            Require(spiderlingCount == 1,
                "summon frame creates one Spiderling");

            Require(session.Enemies.RequestSpiderlingSummon(mother) &&
                session.Enemies.RequestSpiderlingSummon(mother) &&
                !session.Enemies.RequestSpiderlingSummon(mother),
                "pending requests obey per-Mother summon cap");
            Tick(session, 0.01f);
            spiderlingCount = 0;
            foreach (Enemy enemy in session.Enemies.Enemies)
            {
                if (enemy is Spiderling child &&
                    ReferenceEquals(child.Mother, mother))
                {
                    spiderlingCount++;
                }
            }
            Require(spiderlingCount == MotherSpider.MaximumActiveSpiderlings &&
                !session.Enemies.RequestSpiderlingSummon(mother),
                "active Spiderlings remain capped at three");

            mother.ReceiveDamage(int.MaxValue);
            Tick(session, 0.1f);
            Require(session.Enemies.DefeatPresentations.Count == 1 &&
                session.Enemies.DefeatPresentations[0] == mother,
                "Mother Spider collapse persists after combat removal");
            foreach (Enemy enemy in session.Enemies.Enemies)
            {
                Require(enemy is not Spiderling child ||
                    !ReferenceEquals(child.Mother, mother),
                    "orphaned Spiderlings are cleaned with Mother");
            }
            Tick(session, 0.6f);
            Require(session.Enemies.DefeatPresentations.Count == 0,
                "Mother Spider death presentation cleans up once");

            var bossSession = new GameSession(
                new Rectangle(0, 0, 800, 480),
                randomSeed: 743);
            Press(bossSession, Keys.Enter);
            var boss = bossSession.Boss;
            Require(!boss.IsActivated,
                "Ancient Treant starts inactive at showcase distance");
            bossSession.Player.MoveTo(
                boss.Position + new Vector2(-300f, 0f));

            for (int frame = 0;
                 frame < 130 && bossSession.RootHazards.Hazards.Count == 0;
                 frame++)
            {
                Tick(bossSession, 1f / 60f);
            }

            Require(boss.IsActivated &&
                bossSession.RootHazards.Hazards.Count >= 1,
                "nearby boss activates and creates root warning");
            RootHazard bossRoot = bossSession.RootHazards.Hazards[0];
            Require(ReferenceEquals(bossRoot.Owner, boss) &&
                bossRoot.State == RootHazardState.Telegraph &&
                boss.RootVisualState == AncientTreantRootVisualState.Windup,
                "boss pose and root ground warning share a clock");

            int phaseTwoDamage = boss.MaxHealth -
                boss.MaxHealth * 65 / 100 + 1;
            boss.ReceiveDamage(phaseTwoDamage);
            Require(boss.Phase == AncientTreantPhase.PhaseTwo &&
                boss.IsPhaseTransitioning,
                "65 percent threshold starts Phase 2 transition");
            Tick(bossSession, AncientTreant.PhaseTransitionSeconds + 0.01f);
            Require(!boss.IsPhaseTransitioning,
                "Phase 2 transition completes once");

            int phaseThreeTarget = boss.MaxHealth * 30 / 100;
            boss.ReceiveDamage(boss.CurrentHealth - phaseThreeTarget + 1);
            Require(boss.Phase == AncientTreantPhase.PhaseThree &&
                boss.IsPhaseTransitioning,
                "30 percent threshold starts Phase 3 transition");
            Tick(bossSession, AncientTreant.PhaseTransitionSeconds + 0.01f);
            Require(!boss.IsPhaseTransitioning,
                "Phase 3 transition completes once");

            boss.ReceiveDamage(int.MaxValue);
            Tick(bossSession, 0.1f);
            Require(bossSession.RootHazards.Hazards.Count == 0 &&
                boss.DeathPresentationProgress > 0f &&
                !bossSession.BossDefeated,
                "showcase boss death clears roots without normal rewards");
            Tick(bossSession,
                AncientTreant.DeathPresentationSeconds + 0.01f);
            Require(boss.DeathPresentationProgress >= 1f,
                "boss death settles once without returning to idle");
            Require(GameSession.DebugWildForestShowcase,
                "showcase remains enabled during final-phase validation");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static GameTime Frame(float seconds)
    {
        return new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame(1f / 60f), new KeyboardState(key), default);
        session.Update(Frame(1f / 60f), new KeyboardState(), default);
        if (key == Keys.Enter)
        {
            for (int frame = 0;
                 frame < 60 && session.IsMapEntryFallActive;
                 frame++)
            {
                session.Update(
                    Frame(1f / 60f),
                    new KeyboardState(),
                    default);
            }

            Require(!session.IsMapEntryFallActive && session.Player.IsGrounded,
                "entry fall settles before enemy visual assertions");
        }
    }

    private static void Tick(GameSession session, float seconds)
    {
        session.Update(Frame(seconds), new KeyboardState(), default);
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Enemy visual validation failed: {scenario}.");
        }
    }
}
