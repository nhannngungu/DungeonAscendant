using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Converts read-only gameplay state into a reusable presentation state. Attack
/// clips use MeleeAttack phase progress, so their active frames cannot drift
/// away from gameplay timing.
/// </summary>
public sealed class EnemyAnimationController
{
    private readonly EnemyVisualProfile _profile;
    private float _elapsed;

    public EnemyVisualState State { get; private set; } = EnemyVisualState.Idle;
    public int Frame => _profile.GetAnimation(State).GetFrame(_elapsed);
    public float Progress => _profile.GetAnimation(State).GetProgress(_elapsed);
    public float Elapsed => _elapsed;

    public EnemyAnimationController(EnemyVisualProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public void Update(GameTime gameTime, Enemy enemy)
    {
        EnemyVisualState nextState = SelectState(enemy);
        bool changed = nextState != State;

        if (changed)
        {
            State = nextState;
            _elapsed = 0f;
        }

        SpriteAnimation animation = _profile.GetAnimation(State);

        if (enemy is GiantSpider spider && UsesWebAttackClock(State))
        {
            _elapsed = spider.WebPhaseProgress * animation.Duration;
            return;
        }

        if (enemy is ThornCrawler crawler && UsesCrawlerStateClock(State))
        {
            _elapsed = crawler.StateProgress * animation.Duration;
            return;
        }

        if (enemy is BloodBat bat &&
            bat.State == BloodBatState.Retreating &&
            State == EnemyVisualState.DiveRecovery)
        {
            _elapsed = bat.StateProgress * animation.Duration;
            return;
        }

        if (enemy is CorruptedTreant treant && UsesRootStrikeClock(State))
        {
            _elapsed = treant.RootVisualProgress * animation.Duration;
            return;
        }

        if (enemy is GoblinChief chief && UsesWarCryClock(State))
        {
            _elapsed = chief.WarCryProgress * animation.Duration;
            return;
        }

        if (enemy is MotherSpider mother && UsesMotherSpecialClock(State))
        {
            _elapsed = mother.SpecialStateProgress * animation.Duration;
            return;
        }

        if (enemy is CatacombEnemy catacomb &&
            catacomb.IsPolishedCatacombEnemy &&
            catacomb.CombatState != CatacombCombatState.Ready &&
            catacomb.CombatState != CatacombCombatState.GuardApproach &&
            catacomb.CombatState != CatacombCombatState.KnightApproach &&
            catacomb.CombatState != CatacombCombatState.SoulApproach &&
            State is not EnemyVisualState.Hurt and
                not EnemyVisualState.Stagger and
                not EnemyVisualState.Death and
                not EnemyVisualState.BlockReaction)
        {
            _elapsed = catacomb.ActionProgress * animation.Duration;
            return;
        }

        if (UsesAttackClock(State))
        {
            _elapsed = enemy.Attack.PhaseProgress * animation.Duration;
            return;
        }

        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 10f);

        if (!changed || State == EnemyVisualState.Death)
            _elapsed += elapsedSeconds;
    }

    private static EnemyVisualState SelectState(Enemy enemy)
    {
        if (!enemy.IsAlive)
            return EnemyVisualState.Death;

        if (enemy is CatacombEnemy guardReaction &&
            guardReaction.Type == EnemyType.UndeadGuard)
        {
            if (guardReaction.IsGuardBroken)
                return EnemyVisualState.GuardBreak;
            if (guardReaction.IsGuardBlockReacting)
                return EnemyVisualState.BlockReaction;
        }

        if (enemy is CatacombEnemy brokenKnight &&
            brokenKnight.Type == EnemyType.CursedKnight &&
            brokenKnight.IsCursedArmorBroken)
        {
            return EnemyVisualState.CursedArmorBreak;
        }

        if (enemy.IsStaggered)
            return EnemyVisualState.Stagger;

        if (enemy.IsHitFlashing)
            return EnemyVisualState.Hurt;

        if (enemy is CatacombEnemy rotten &&
            rotten.Type == EnemyType.RottenCorpse)
        {
            return rotten.CombatState switch
            {
                CatacombCombatState.CorpseWindup when
                    rotten.RottenCorpseAttack ==
                        RottenCorpseAttackKind.BodySlam =>
                    EnemyVisualState.BodySlamWindup,
                CatacombCombatState.CorpseWindup when
                    rotten.RottenCorpseAttack ==
                        RottenCorpseAttackKind.RotVomit =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.CorpseWindup =>
                    EnemyVisualState.GrabWindup,
                CatacombCombatState.CorpseActive when
                    rotten.RottenCorpseAttack ==
                        RottenCorpseAttackKind.BodySlam =>
                    EnemyVisualState.BodySlamAttack,
                CatacombCombatState.CorpseActive when
                    rotten.RottenCorpseAttack ==
                        RottenCorpseAttackKind.RotVomit =>
                    EnemyVisualState.RotVomit,
                CatacombCombatState.CorpseActive =>
                    EnemyVisualState.GrabAttack,
                CatacombCombatState.CorpseRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Walk
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is CatacombEnemy wraith &&
            wraith.Type == EnemyType.Wraith)
        {
            return wraith.CombatState switch
            {
                CatacombCombatState.WraithEthereal =>
                    EnemyVisualState.Ethereal,
                CatacombCombatState.WraithMaterialize =>
                    EnemyVisualState.Materialize,
                CatacombCombatState.WraithFade =>
                    EnemyVisualState.PhaseOut,
                CatacombCombatState.WraithWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.WraithActive when
                    wraith.WraithAttack == WraithAttackKind.SpectralDash =>
                    EnemyVisualState.SpectralDash,
                CatacombCombatState.WraithActive when
                    wraith.WraithAttack == WraithAttackKind.CurseWave =>
                    EnemyVisualState.CurseWave,
                CatacombCombatState.WraithActive =>
                    EnemyVisualState.SpectralClaw,
                CatacombCombatState.WraithRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.IdleHover
            };
        }

        if (enemy is CatacombEnemy guard &&
            guard.Type == EnemyType.UndeadGuard)
        {
            return guard.CombatState switch
            {
                CatacombCombatState.GuardApproach =>
                    MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Walk
                        : EnemyVisualState.ShieldReady,
                CatacombCombatState.GuardStance =>
                    EnemyVisualState.ShieldReady,
                CatacombCombatState.GuardWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.GuardActive when
                    guard.UndeadGuardAttack ==
                        UndeadGuardAttackKind.ShieldBash =>
                    EnemyVisualState.ShieldBash,
                CatacombCombatState.GuardActive when
                    guard.UndeadGuardAttack ==
                        UndeadGuardAttackKind.HeavyCleave =>
                    EnemyVisualState.HeavyMeleeAttack,
                CatacombCombatState.GuardActive =>
                    EnemyVisualState.GuardedStrike,
                CatacombCombatState.GuardRecovery =>
                    EnemyVisualState.AttackRecovery,
                CatacombCombatState.GuardBroken =>
                    EnemyVisualState.GuardBreak,
                _ => EnemyVisualState.ShieldReady
            };
        }

        if (enemy is CatacombEnemy graveBat &&
            graveBat.Type == EnemyType.GraveBat)
        {
            return graveBat.CombatState switch
            {
                CatacombCombatState.GraveHover =>
                    EnemyVisualState.GraveHover,
                CatacombCombatState.GraveCircle =>
                    EnemyVisualState.GraveCircle,
                CatacombCombatState.GraveWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.GraveActive when
                    graveBat.GraveBatAttack ==
                        GraveBatAttackKind.GraveScreech =>
                    EnemyVisualState.GraveScreech,
                CatacombCombatState.GraveActive when
                    graveBat.GraveBatAttack ==
                        GraveBatAttackKind.SwarmFeint =>
                    EnemyVisualState.GraveFeint,
                CatacombCombatState.GraveActive =>
                    EnemyVisualState.GraveClawPass,
                CatacombCombatState.GraveRecovery =>
                    EnemyVisualState.AttackRecovery,
                CatacombCombatState.GraveRetreat =>
                    EnemyVisualState.GraveRetreat,
                _ => EnemyVisualState.GraveHover
            };
        }

        if (enemy is CatacombEnemy knight &&
            knight.Type == EnemyType.CursedKnight)
        {
            return knight.CombatState switch
            {
                CatacombCombatState.KnightDormant =>
                    EnemyVisualState.KnightDormant,
                CatacombCombatState.KnightApproach =>
                    MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Walk
                        : EnemyVisualState.SwordReady,
                CatacombCombatState.KnightReady =>
                    EnemyVisualState.SwordReady,
                CatacombCombatState.KnightWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.KnightActive when
                    knight.CursedKnightAttack ==
                        CursedKnightAttackKind.ExecutionStrike =>
                    EnemyVisualState.ExecutionStrike,
                CatacombCombatState.KnightActive when
                    knight.CursedKnightAttack ==
                        CursedKnightAttackKind.AdvancingThrust =>
                    EnemyVisualState.AdvancingThrust,
                CatacombCombatState.KnightActive =>
                    EnemyVisualState.CursedSweep,
                CatacombCombatState.KnightRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.SwordReady
            };
        }

        if (enemy is CatacombEnemy deathKnight &&
            deathKnight.Type == EnemyType.DeathKnight)
        {
            return deathKnight.CombatState switch
            {
                CatacombCombatState.KnightDormant =>
                    EnemyVisualState.KnightDormant,
                CatacombCombatState.KnightApproach =>
                    MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Walk
                        : EnemyVisualState.SwordReady,
                CatacombCombatState.KnightReady =>
                    EnemyVisualState.SwordReady,
                CatacombCombatState.KnightWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.KnightActive when
                    deathKnight.DeathKnightAttack ==
                        DeathKnightAttackKind.ExecutionCrush =>
                    EnemyVisualState.ExecutionCrush,
                CatacombCombatState.KnightActive when
                    deathKnight.DeathKnightAttack ==
                        DeathKnightAttackKind.DreadCharge =>
                    EnemyVisualState.DreadCharge,
                CatacombCombatState.KnightActive when
                    deathKnight.DeathKnightAttack ==
                        DeathKnightAttackKind.TombbreakerSlam =>
                    EnemyVisualState.TombbreakerSlam,
                CatacombCombatState.KnightActive => EnemyVisualState.WarSweep,
                CatacombCombatState.KnightRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.SwordReady
            };
        }

        if (enemy is CatacombEnemy fallenKnight &&
            fallenKnight.Type == EnemyType.FallenKnight)
        {
            return fallenKnight.CombatState switch
            {
                CatacombCombatState.KnightDormant =>
                    EnemyVisualState.KnightDormant,
                CatacombCombatState.KnightTransition =>
                    EnemyVisualState.KnightPhaseTransition,
                CatacombCombatState.KnightApproach =>
                    MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Walk
                        : EnemyVisualState.SwordReady,
                CatacombCombatState.KnightReady =>
                    EnemyVisualState.SwordReady,
                CatacombCombatState.KnightWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.KnightActive =>
                    FallenKnightActiveVisual(fallenKnight.FallenKnightAttack),
                CatacombCombatState.KnightRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.SwordReady
            };
        }

        if (enemy is CatacombEnemy collector &&
            collector.Type == EnemyType.SoulCollector)
        {
            return collector.CombatState switch
            {
                CatacombCombatState.SoulApproach =>
                    MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Move
                        : EnemyVisualState.RitualIdle,
                CatacombCombatState.SoulReady => EnemyVisualState.RitualIdle,
                CatacombCombatState.SoulWindup => EnemyVisualState.StaffRaise,
                CatacombCombatState.SoulActive when
                    collector.SoulCollectorAttack ==
                        SoulCollectorAttackKind.RitualCurseField =>
                    EnemyVisualState.RitualField,
                CatacombCombatState.SoulActive when
                    collector.SoulCollectorAttack ==
                        SoulCollectorAttackKind.GravePull =>
                    EnemyVisualState.GravePull,
                CatacombCombatState.SoulActive when
                    collector.SoulCollectorAttack ==
                        SoulCollectorAttackKind.SoulBurst =>
                    EnemyVisualState.SoulBurst,
                CatacombCombatState.SoulActive =>
                    EnemyVisualState.SoulBoltCast,
                CatacombCombatState.SoulRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.RitualIdle
            };
        }

        if (enemy is GoblinHunter)
        {
            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.Aim,
                EnemyAttackPhase.Active => EnemyVisualState.Shoot,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => enemy is GoblinHunter hunter && hunter.IsRetreating
                    ? EnemyVisualState.Retreat
                    : MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Move
                        : EnemyVisualState.Idle
            };
        }

        if (enemy is CatacombEnemy catacomb &&
            catacomb.IsPolishedSkeleton)
        {
            if (catacomb.Type == EnemyType.SkeletonArcher)
            {
                return catacomb.CombatState switch
                {
                    CatacombCombatState.ArcherNock => EnemyVisualState.Aim,
                    CatacombCombatState.ArcherDraw => EnemyVisualState.Aim,
                    CatacombCombatState.ArcherRelease => EnemyVisualState.Shoot,
                    CatacombCombatState.ArcherRecovery =>
                        EnemyVisualState.AttackRecovery,
                    CatacombCombatState.BowShoveWindup =>
                        EnemyVisualState.AttackWindup,
                    CatacombCombatState.BowShoveActive => EnemyVisualState.Attack,
                    CatacombCombatState.BowShoveRecovery =>
                        EnemyVisualState.AttackRecovery,
                    _ => catacomb.IsRetreating
                        ? EnemyVisualState.Retreat
                        : MathF.Abs(enemy.VisualVelocityX) > 1f
                            ? EnemyVisualState.Move
                            : EnemyVisualState.Idle
                };
            }

            return catacomb.CombatState switch
            {
                CatacombCombatState.MeleeWindup =>
                    EnemyVisualState.AttackWindup,
                CatacombCombatState.MeleeActive when
                    catacomb.SkeletonAttack ==
                        SkeletonAttackKind.OverheadChop =>
                    EnemyVisualState.HeavyMeleeAttack,
                CatacombCombatState.MeleeActive => EnemyVisualState.Attack,
                CatacombCombatState.MeleeRecovery =>
                    EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Walk
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is BloodBat bat)
        {
            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.DiveWindup,
                EnemyAttackPhase.Active => EnemyVisualState.DiveAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.DiveRecovery,
                _ => bat.State switch
                {
                    BloodBatState.Diving => bat.StateProgress < 0.42f
                        ? EnemyVisualState.Approach
                        : EnemyVisualState.LowAltitude,
                    BloodBatState.Retreating => EnemyVisualState.DiveRecovery,
                    _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Fly
                        : EnemyVisualState.IdleHover
                }
            };
        }

        if (enemy is ThornCrawler crawler)
        {
            if (crawler.State == ThornCrawlerState.Attacking)
            {
                return enemy.Attack.Phase switch
                {
                    EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                    EnemyAttackPhase.Active => EnemyVisualState.Attack,
                    EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                    _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                        ? EnemyVisualState.Move
                        : EnemyVisualState.Idle
                };
            }

            return crawler.State switch
            {
                ThornCrawlerState.Hidden => EnemyVisualState.Hidden,
                ThornCrawlerState.TrackingUnderground => EnemyVisualState.Hidden,
                ThornCrawlerState.Warning => EnemyVisualState.Warning,
                ThornCrawlerState.Emerging => EnemyVisualState.Emerging,
                ThornCrawlerState.Recovering => EnemyVisualState.Recovery,
                ThornCrawlerState.Burrowing => EnemyVisualState.Burrow,
                _ => EnemyVisualState.Idle
            };
        }

        if (enemy is CorruptedTreant treant)
        {
            EnemyVisualState meleeState = enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.HeavyMeleeAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => EnemyVisualState.Idle
            };

            if (enemy.Attack.Phase == EnemyAttackPhase.Windup ||
                enemy.Attack.Phase == EnemyAttackPhase.Active ||
                enemy.Attack.Phase == EnemyAttackPhase.Recovery)
            {
                return meleeState;
            }

            return treant.RootVisualState switch
            {
                CorruptedTreantRootVisualState.Windup =>
                    EnemyVisualState.RootStrikeWindup,
                CorruptedTreantRootVisualState.Strike =>
                    EnemyVisualState.RootStrike,
                CorruptedTreantRootVisualState.Recovery =>
                    EnemyVisualState.RootStrikeRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Walk
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is GoblinChief chief)
        {
            if (chief.WarCryState != GoblinChiefWarCryState.Ready)
            {
                return chief.WarCryState switch
                {
                    GoblinChiefWarCryState.Windup =>
                        EnemyVisualState.WarCryWindup,
                    GoblinChiefWarCryState.Cry => EnemyVisualState.WarCry,
                    _ => EnemyVisualState.WarCryRecovery
                };
            }

            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.Attack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Walk
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is GiantSpider spider)
        {
            EnemyVisualState webState = spider.WebState switch
            {
                GiantSpiderWebState.Prepare => EnemyVisualState.WebPrepare,
                GiantSpiderWebState.Shoot => EnemyVisualState.WebShoot,
                GiantSpiderWebState.Recovery => EnemyVisualState.WebRecovery,
                _ => EnemyVisualState.Idle
            };

            if (spider.WebState != GiantSpiderWebState.Ready)
                return webState;

            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.BiteAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Move
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is MotherSpider mother)
        {
            if (mother.SpecialState != MotherSpiderSpecialState.Ready)
            {
                return mother.SpecialState switch
                {
                    MotherSpiderSpecialState.WebPrepare =>
                        EnemyVisualState.WebPrepare,
                    MotherSpiderSpecialState.WebShoot =>
                        EnemyVisualState.WebShoot,
                    MotherSpiderSpecialState.WebRecovery =>
                        EnemyVisualState.WebRecovery,
                    MotherSpiderSpecialState.SummonWindup =>
                        EnemyVisualState.SummonWindup,
                    MotherSpiderSpecialState.Summon =>
                        EnemyVisualState.SummonSpiderlings,
                    _ => EnemyVisualState.SummonRecovery
                };
            }

            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.BiteAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Crawl
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is Spiderling)
        {
            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.BiteAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                    ? EnemyVisualState.Crawl
                    : EnemyVisualState.Idle
            };
        }

        if (enemy is DireWolf wolf)
        {
            return enemy.Attack.Phase switch
            {
                EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
                EnemyAttackPhase.Active => EnemyVisualState.LungeAttack,
                EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
                _ => wolf.IsRetreating
                    ? EnemyVisualState.Retreat
                    : MathF.Abs(enemy.VisualVelocityX) > 135f
                        ? EnemyVisualState.Run
                        : MathF.Abs(enemy.VisualVelocityX) > 1f
                            ? EnemyVisualState.Prowl
                            : EnemyVisualState.Idle
            };
        }

        return enemy.Attack.Phase switch
        {
            EnemyAttackPhase.Windup => EnemyVisualState.AttackWindup,
            EnemyAttackPhase.Active => EnemyVisualState.Attack,
            EnemyAttackPhase.Recovery => EnemyVisualState.AttackRecovery,
            _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                ? EnemyVisualState.Move
                : EnemyVisualState.Idle
        };
    }

    private static EnemyVisualState FallenKnightActiveVisual(
        FallenKnightAttackKind attack) => attack switch
        {
            FallenKnightAttackKind.GuardedCounter or
                FallenKnightAttackKind.CursedCounter =>
                EnemyVisualState.KnightCounter,
            FallenKnightAttackKind.AdvancingThrust =>
                EnemyVisualState.AdvancingThrust,
            FallenKnightAttackKind.HeavyOverhead =>
                EnemyVisualState.ExecutionCrush,
            FallenKnightAttackKind.CursedExtensionSlash =>
                EnemyVisualState.CursedExtensionSlash,
            FallenKnightAttackKind.GraveStep => EnemyVisualState.GraveStep,
            FallenKnightAttackKind.FinalOathCleave =>
                EnemyVisualState.FinalOathCleave,
            FallenKnightAttackKind.OathbreakerRush =>
                EnemyVisualState.OathbreakerRush,
            FallenKnightAttackKind.LastJudgment =>
                EnemyVisualState.LastJudgment,
            _ => EnemyVisualState.RoyalSlash
        };

    private static bool UsesAttackClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.AttackWindup ||
            state == EnemyVisualState.Attack ||
            state == EnemyVisualState.HeavyMeleeAttack ||
            state == EnemyVisualState.LungeAttack ||
            state == EnemyVisualState.BiteAttack ||
            state == EnemyVisualState.DiveWindup ||
            state == EnemyVisualState.DiveAttack ||
            state == EnemyVisualState.DiveRecovery ||
            state == EnemyVisualState.AttackRecovery ||
            state == EnemyVisualState.Aim ||
            state == EnemyVisualState.Shoot;
    }

    private static bool UsesRootStrikeClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.RootStrikeWindup ||
            state == EnemyVisualState.RootStrike ||
            state == EnemyVisualState.RootStrikeRecovery;
    }

    private static bool UsesWarCryClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.WarCryWindup ||
            state == EnemyVisualState.WarCry ||
            state == EnemyVisualState.WarCryRecovery;
    }

    private static bool UsesMotherSpecialClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.WebPrepare ||
            state == EnemyVisualState.WebShoot ||
            state == EnemyVisualState.WebRecovery ||
            state == EnemyVisualState.SummonWindup ||
            state == EnemyVisualState.SummonSpiderlings ||
            state == EnemyVisualState.SummonRecovery;
    }

    private static bool UsesCrawlerStateClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.Warning ||
            state == EnemyVisualState.Emerging ||
            state == EnemyVisualState.Recovery ||
            state == EnemyVisualState.Burrow;
    }

    private static bool UsesWebAttackClock(EnemyVisualState state)
    {
        return state == EnemyVisualState.WebPrepare ||
            state == EnemyVisualState.WebShoot ||
            state == EnemyVisualState.WebRecovery;
    }
}
