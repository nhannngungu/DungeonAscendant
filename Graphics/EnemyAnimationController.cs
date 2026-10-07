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

        if (enemy.IsStaggered)
            return EnemyVisualState.Stagger;

        if (enemy.IsHitFlashing)
            return EnemyVisualState.Hurt;

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
