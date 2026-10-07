namespace DungeonAscendant.Combat;

/// <summary>
/// Stamina remains the only live resource. MagicReady marks attacks that can
/// move to a dedicated magic pool later without changing moveset data.
/// </summary>
public enum AttackResourceKind
{
    Stamina,
    MagicReady
}
