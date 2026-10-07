namespace DungeonAscendant.Items;

public static class WeaponFamilyExtensions
{
    public static string ToDisplayName(this WeaponFamily family)
    {
        return family switch
        {
            WeaponFamily.LongSword => "Long Sword",
            WeaponFamily.GreatSword => "Great Sword",
            WeaponFamily.BattleAxe => "Battle Axe",
            WeaponFamily.DualDaggers => "Dual Daggers",
            WeaponFamily.ArcaneStaff => "Arcane Staff",
            _ => family.ToString()
        };
    }
}
