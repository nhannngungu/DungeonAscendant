namespace DungeonAscendant.Combat;

/// <summary>
/// Compatibility facade for systems that need the balanced default attack.
/// </summary>
public static class SwordAttackSet
{
    public static AttackDefinition LightOne => WeaponMoveSets.LongSword.GetLight(0);
    public static AttackDefinition LightTwo => WeaponMoveSets.LongSword.GetLight(1);
    public static AttackDefinition LightThree => WeaponMoveSets.LongSword.GetLight(2);
    public static AttackDefinition Heavy => WeaponMoveSets.LongSword.Heavy;

    public static AttackDefinition GetLight(int index)
    {
        return WeaponMoveSets.LongSword.GetLight(index);
    }
}
