namespace DungeonAscendant.Player;

/// <summary>
/// Presentation-only states consumed by the player sprite renderer. Combat
/// remains authoritative; this enum never drives gameplay transitions.
/// </summary>
public enum PlayerVisualState
{
    Idle,
    Run,
    Jump,
    Fall,
    LightAttack1,
    LightAttack2,
    LightAttack3,
    LightAttack4,
    LightAttack5,
    HeavyCharge,
    HeavyAttack,
    Dodge,
    Block,
    GuardBreak,
    Hurt,
    Dead,
    PotionUse
}
