namespace DungeonAscendant.Player;

/// <summary>
/// Minimal presentation contract for primitive rendering now and sprite
/// animation later. Combat state remains owned by the attack system.
/// </summary>
public enum PlayerVisualState
{
    Idle,
    Run,
    Jump,
    Fall,
    Hurt,
    Death
}
