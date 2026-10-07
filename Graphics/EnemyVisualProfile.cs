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
}
