using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Immutable visual dimensions and elapsed-time animation clips for an enemy.
/// Profiles are independent of collision sizes and can later point at sprite
/// sheet cells without changing AI or combat code.
/// </summary>
public sealed class EnemyVisualProfile
{
    private readonly IReadOnlyDictionary<EnemyVisualState, SpriteAnimation>
        _animations;

    public EnemyType EnemyType { get; }
    public Vector2 VisualSize { get; }

    public EnemyVisualProfile(
        EnemyType enemyType,
        Vector2 visualSize,
        IReadOnlyDictionary<EnemyVisualState, SpriteAnimation> animations)
    {
        EnemyType = enemyType;
        VisualSize = visualSize;
        _animations = animations ??
            throw new ArgumentNullException(nameof(animations));
    }

    public SpriteAnimation GetAnimation(EnemyVisualState state)
    {
        return _animations.TryGetValue(state, out SpriteAnimation animation)
            ? animation
            : _animations[EnemyVisualState.Idle];
    }

    public static EnemyVisualProfile Goblin { get; } = new(
        EnemyType.Goblin,
        new Vector2(52f, 50f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.12f, true),
            [EnemyVisualState.Move] = new(6, 0.075f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.12f, false),
            [EnemyVisualState.Attack] = new(3, 0.0567f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.10f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.164f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile GoblinHunter { get; } = new(
        EnemyType.GoblinHunter,
        new Vector2(56f, 57f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.14f, true),
            [EnemyVisualState.Move] = new(6, 0.095f, true),
            [EnemyVisualState.Retreat] = new(6, 0.085f, true),
            [EnemyVisualState.Aim] = new(4, 0.065f, false),
            [EnemyVisualState.Shoot] = new(2, 0.03f, false),
            [EnemyVisualState.AttackRecovery] = new(3, 0.06f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.136f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile DireWolf { get; } = new(
        EnemyType.DireWolf,
        new Vector2(100f, 44f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.14f, true),
            [EnemyVisualState.Prowl] = new(8, 0.095f, true),
            [EnemyVisualState.Run] = new(8, 0.0625f, true),
            [EnemyVisualState.Retreat] = new(8, 0.0525f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.06f, false),
            [EnemyVisualState.LungeAttack] = new(3, 0.0334f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.05f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.116f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile GiantSpider { get; } = new(
        EnemyType.GiantSpider,
        new Vector2(100f, 36f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.16f, true),
            [EnemyVisualState.Move] = new(8, 0.09f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.065f, false),
            [EnemyVisualState.BiteAttack] = new(3, 0.0334f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.06f, false),
            [EnemyVisualState.WebPrepare] = new(6, 0.06f, false),
            [EnemyVisualState.WebShoot] = new(2, 0.035f, false),
            [EnemyVisualState.WebRecovery] = new(4, 0.0625f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.13f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile BloodBat { get; } = new(
        EnemyType.BloodBat,
        new Vector2(92f, 42f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.12f, true),
            [EnemyVisualState.IdleHover] = new(6, 0.12f, true),
            [EnemyVisualState.Fly] = new(8, 0.075f, true),
            [EnemyVisualState.Approach] = new(6, 0.07f, true),
            [EnemyVisualState.DiveWindup] = new(4, 0.0275f, false),
            [EnemyVisualState.DiveAttack] = new(3, 0.0234f, false),
            [EnemyVisualState.DiveRecovery] = new(3, 0.05f, false),
            [EnemyVisualState.LowAltitude] = new(6, 0.08f, true),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.136f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile ThornCrawler { get; } = new(
        EnemyType.ThornCrawler,
        new Vector2(68f, 30f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(6, 0.15f, true),
            [EnemyVisualState.Hidden] = new(6, 0.15f, true),
            [EnemyVisualState.Warning] = new(5, 0.13f, false),
            [EnemyVisualState.Emerging] = new(5, 0.07f, false),
            [EnemyVisualState.Move] = new(6, 0.10f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.055f, false),
            [EnemyVisualState.Attack] = new(3, 0.03f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.06f, false),
            [EnemyVisualState.Recovery] = new(6, 0.15f, false),
            [EnemyVisualState.Burrow] = new(5, 0.09f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.136f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile CorruptedTreant { get; } = new(
        EnemyType.CorruptedTreant,
        new Vector2(108f, 106f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, 0.18f, true),
            [EnemyVisualState.Walk] = new(8, 0.12f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.095f, false),
            [EnemyVisualState.HeavyMeleeAttack] = new(3, 0.0434f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.095f, false),
            [EnemyVisualState.RootStrikeWindup] = new(5, 0.17f, false),
            [EnemyVisualState.RootStrike] = new(3, 0.06f, false),
            [EnemyVisualState.RootStrikeRecovery] = new(5, 0.084f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.084f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile GoblinChief { get; } = new(
        EnemyType.GoblinChief,
        new Vector2(78f, 71f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, 0.12f, true),
            [EnemyVisualState.Walk] = new(8, 0.09f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.06f, false),
            [EnemyVisualState.Attack] = new(3, 0.0334f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.065f, false),
            [EnemyVisualState.WarCryWindup] = new(4, 0.08f, false),
            [EnemyVisualState.WarCry] = new(3, 0.06f, false),
            [EnemyVisualState.WarCryRecovery] = new(5, 0.06f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.104f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile MotherSpider { get; } = new(
        EnemyType.MotherSpider,
        new Vector2(140f, 52f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, 0.18f, true),
            [EnemyVisualState.Crawl] = new(8, 0.11f, true),
            [EnemyVisualState.AttackWindup] = new(4, 0.07f, false),
            [EnemyVisualState.BiteAttack] = new(3, 0.0367f, false),
            [EnemyVisualState.AttackRecovery] = new(4, 0.075f, false),
            [EnemyVisualState.WebPrepare] = new(6, 0.07f, false),
            [EnemyVisualState.WebShoot] = new(2, 0.04f, false),
            [EnemyVisualState.WebRecovery] = new(5, 0.06f, false),
            [EnemyVisualState.SummonWindup] = new(5, 0.11f, false),
            [EnemyVisualState.SummonSpiderlings] = new(3, 0.06f, false),
            [EnemyVisualState.SummonRecovery] = new(5, 0.07f, false),
            [EnemyVisualState.Hurt] = new(3, 0.047f, false),
            [EnemyVisualState.Stagger] = new(5, 0.092f, false),
            [EnemyVisualState.Death] = new(6, 0.095f, false)
        });

    public static EnemyVisualProfile Spiderling { get; } = new(
        EnemyType.Spiderling,
        new Vector2(36f, 18f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(4, 0.12f, true),
            [EnemyVisualState.Crawl] = new(6, 0.065f, true),
            [EnemyVisualState.AttackWindup] = new(3, 0.0767f, false),
            [EnemyVisualState.BiteAttack] = new(2, 0.04f, false),
            [EnemyVisualState.AttackRecovery] = new(3, 0.0634f, false),
            [EnemyVisualState.Hurt] = new(2, 0.07f, false),
            [EnemyVisualState.Stagger] = new(4, 0.17f, false),
            [EnemyVisualState.Death] = new(4, 0.1425f, false)
        });

    public static EnemyVisualProfile Skeleton { get; } = new(
        EnemyType.Skeleton,
        new Vector2(76f, 68f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .14f, true),
            [EnemyVisualState.Walk] = new(8, .10f, true),
            [EnemyVisualState.AttackWindup] = new(5, .10f, false),
            [EnemyVisualState.Attack] = new(3, .04f, false),
            [EnemyVisualState.HeavyMeleeAttack] = new(4, .035f, false),
            [EnemyVisualState.AttackRecovery] = new(5, .10f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(6, .12f, false),
            [EnemyVisualState.Death] = new(7, .0815f, false)
        });

    public static EnemyVisualProfile SkeletonArcher { get; } = new(
        EnemyType.SkeletonArcher,
        new Vector2(88f, 72f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .17f, true),
            [EnemyVisualState.Move] = new(8, .12f, true),
            [EnemyVisualState.Retreat] = new(8, .105f, true),
            [EnemyVisualState.Aim] = new(8, .10f, false),
            [EnemyVisualState.Shoot] = new(2, .04f, false),
            [EnemyVisualState.AttackWindup] = new(4, .0625f, false),
            [EnemyVisualState.Attack] = new(3, .034f, false),
            [EnemyVisualState.AttackRecovery] = new(5, .09f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(6, .115f, false),
            [EnemyVisualState.Death] = new(7, .0815f, false)
        });

    public static EnemyVisualProfile RottenCorpse { get; } = new(
        EnemyType.RottenCorpse,
        new Vector2(92f, 78f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .18f, true),
            [EnemyVisualState.Walk] = new(8, .145f, true),
            [EnemyVisualState.GrabWindup] = new(6, .1034f, false),
            [EnemyVisualState.GrabAttack] = new(3, .0534f, false),
            [EnemyVisualState.BodySlamWindup] = new(7, .1172f, false),
            [EnemyVisualState.BodySlamAttack] = new(3, .06f, false),
            [EnemyVisualState.AttackWindup] = new(7, .1058f, false),
            [EnemyVisualState.RotVomit] = new(7, .04f, false),
            [EnemyVisualState.AttackRecovery] = new(6, .12f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(7, .115f, false),
            [EnemyVisualState.Death] = new(9, .1356f, false)
        });

    public static EnemyVisualProfile Wraith { get; } = new(
        EnemyType.Wraith,
        new Vector2(88f, 82f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .16f, true),
            [EnemyVisualState.IdleHover] = new(8, .16f, true),
            [EnemyVisualState.Ethereal] = new(8, .13f, true),
            [EnemyVisualState.Materialize] = new(6, .08f, false),
            [EnemyVisualState.PhaseOut] = new(5, .068f, false),
            [EnemyVisualState.AttackWindup] = new(6, .10f, false),
            [EnemyVisualState.SpectralClaw] = new(3, .0434f, false),
            [EnemyVisualState.SpectralDash] = new(4, .055f, false),
            [EnemyVisualState.CurseWave] = new(5, .04f, false),
            [EnemyVisualState.AttackRecovery] = new(6, .11f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(6, .12f, false),
            [EnemyVisualState.Death] = new(8, .115f, false)
        });

    public static EnemyVisualProfile UndeadGuard { get; } = new(
        EnemyType.UndeadGuard,
        new Vector2(94f, 78f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .17f, true),
            [EnemyVisualState.Walk] = new(8, .14f, true),
            [EnemyVisualState.ShieldReady] = new(6, .07f, false),
            [EnemyVisualState.AttackWindup] = new(7, .11f, false),
            [EnemyVisualState.GuardedStrike] = new(3, .047f, false),
            [EnemyVisualState.ShieldBash] = new(3, .047f, false),
            [EnemyVisualState.HeavyMeleeAttack] = new(4, .043f, false),
            [EnemyVisualState.AttackRecovery] = new(7, .11f, false),
            [EnemyVisualState.BlockReaction] = new(3, .067f, false),
            [EnemyVisualState.GuardBreak] = new(8, .156f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(7, .11f, false),
            [EnemyVisualState.Death] = new(8, .11f, false)
        });

    public static EnemyVisualProfile GraveBat { get; } = new(
        EnemyType.GraveBat,
        new Vector2(84f, 48f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(7, .16f, true),
            [EnemyVisualState.GraveHover] = new(8, .11f, true),
            [EnemyVisualState.GraveCircle] = new(8, .085f, true),
            [EnemyVisualState.AttackWindup] = new(6, .10f, false),
            [EnemyVisualState.GraveScreech] = new(4, .045f, false),
            [EnemyVisualState.GraveClawPass] = new(5, .048f, false),
            [EnemyVisualState.GraveFeint] = new(4, .045f, false),
            [EnemyVisualState.AttackRecovery] = new(5, .09f, false),
            [EnemyVisualState.GraveRetreat] = new(7, .089f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(5, .136f, false),
            [EnemyVisualState.Death] = new(9, .1134f, false)
        });

    public static EnemyVisualProfile CursedKnight { get; } = new(
        EnemyType.CursedKnight,
        new Vector2(116f, 102f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .18f, true),
            [EnemyVisualState.KnightDormant] = new(7, .103f, false),
            [EnemyVisualState.Walk] = new(8, .15f, true),
            [EnemyVisualState.SwordReady] = new(6, .064f, false),
            [EnemyVisualState.AttackWindup] = new(9, .112f, false),
            [EnemyVisualState.CursedSweep] = new(4, .045f, false),
            [EnemyVisualState.ExecutionStrike] = new(4, .045f, false),
            [EnemyVisualState.AdvancingThrust] = new(5, .052f, false),
            [EnemyVisualState.AttackRecovery] = new(9, .116f, false),
            [EnemyVisualState.CursedArmorBreak] = new(9, .13f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(8, .10f, false),
            [EnemyVisualState.Death] = new(10, .11f, false)
        });

    public static EnemyVisualProfile SoulCollector { get; } = new(
        EnemyType.SoulCollector,
        new Vector2(104f, 104f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .19f, true),
            [EnemyVisualState.RitualIdle] = new(8, .19f, true),
            [EnemyVisualState.Move] = new(8, .14f, true),
            [EnemyVisualState.StaffRaise] = new(8, .11f, false),
            [EnemyVisualState.SoulBoltCast] = new(4, .04f, false),
            [EnemyVisualState.RitualField] = new(4, .04f, false),
            [EnemyVisualState.GravePull] = new(4, .04f, false),
            [EnemyVisualState.SoulBurst] = new(4, .04f, false),
            [EnemyVisualState.AttackRecovery] = new(7, .11f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(7, .11f, false),
            [EnemyVisualState.Death] = new(10, .105f, false)
        });

    public static EnemyVisualProfile DeathKnight { get; } = new(
        EnemyType.DeathKnight,
        new Vector2(142f, 126f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .20f, true),
            [EnemyVisualState.KnightDormant] = new(8, .132f, false),
            [EnemyVisualState.Walk] = new(8, .17f, true),
            [EnemyVisualState.SwordReady] = new(6, .08f, false),
            [EnemyVisualState.AttackWindup] = new(10, .12f, false),
            [EnemyVisualState.WarSweep] = new(5, .04f, false),
            [EnemyVisualState.ExecutionCrush] = new(5, .04f, false),
            [EnemyVisualState.DreadCharge] = new(8, .06f, false),
            [EnemyVisualState.TombbreakerSlam] = new(5, .044f, false),
            [EnemyVisualState.AttackRecovery] = new(9, .125f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(10, .135f, false),
            [EnemyVisualState.Death] = new(14, .116f, false)
        });

    public static EnemyVisualProfile FallenKnight { get; } = new(
        EnemyType.FallenKnight,
        new Vector2(136f, 134f),
        new Dictionary<EnemyVisualState, SpriteAnimation>
        {
            [EnemyVisualState.Idle] = new(8, .18f, true),
            [EnemyVisualState.KnightDormant] = new(9, .13f, false),
            [EnemyVisualState.Walk] = new(8, .13f, true),
            [EnemyVisualState.SwordReady] = new(7, .06f, false),
            [EnemyVisualState.KnightPhaseTransition] = new(10, .105f, false),
            [EnemyVisualState.AttackWindup] = new(9, .105f, false),
            [EnemyVisualState.RoyalSlash] = new(4, .043f, false),
            [EnemyVisualState.KnightCounter] = new(4, .05f, false),
            [EnemyVisualState.AdvancingThrust] = new(5, .05f, false),
            [EnemyVisualState.ExecutionCrush] = new(5, .044f, false),
            [EnemyVisualState.CursedExtensionSlash] = new(5, .044f, false),
            [EnemyVisualState.GraveStep] = new(6, .047f, false),
            [EnemyVisualState.FinalOathCleave] = new(5, .044f, false),
            [EnemyVisualState.OathbreakerRush] = new(10, .062f, false),
            [EnemyVisualState.LastJudgment] = new(6, .04f, false),
            [EnemyVisualState.AttackRecovery] = new(9, .12f, false),
            [EnemyVisualState.Hurt] = new(3, .047f, false),
            [EnemyVisualState.Stagger] = new(8, .1025f, false),
            [EnemyVisualState.Death] = new(16, .128f, false)
        });
}
