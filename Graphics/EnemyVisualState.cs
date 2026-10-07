namespace DungeonAscendant.Graphics;

/// <summary>
/// Presentation-only enemy states. Gameplay remains owned by Enemy and its
/// combat/AI classes; renderers translate that read-only state into poses.
/// </summary>
public enum EnemyVisualState
{
    Idle,
    IdleHover,
    Move,
    Walk,
    Crawl,
    Fly,
    Approach,
    Prowl,
    Run,
    Retreat,
    AttackWindup,
    Attack,
    HeavyMeleeAttack,
    LungeAttack,
    BiteAttack,
    DiveWindup,
    DiveAttack,
    DiveRecovery,
    LowAltitude,
    AttackRecovery,
    Aim,
    Shoot,
    WebPrepare,
    WebShoot,
    WebRecovery,
    SummonWindup,
    SummonSpiderlings,
    SummonRecovery,
    Hidden,
    Warning,
    Emerging,
    Recovery,
    Burrow,
    RootStrikeWindup,
    RootStrike,
    RootStrikeRecovery,
    WarCryWindup,
    WarCry,
    WarCryRecovery,
    Hurt,
    Stagger,
    Death
}
