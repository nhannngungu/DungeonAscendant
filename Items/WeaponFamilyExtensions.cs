namespace DungeonAscendant.Items;

public static class WeaponFamilyExtensions
{
    public static WeaponFamily ToOfficialFamily(this WeaponFamily family)
    {
#pragma warning disable CS0618
        return family switch
        {
            WeaponFamily.GreatSword => WeaponFamily.SpikedMace,
            WeaponFamily.BattleAxe => WeaponFamily.WarAxe,
            WeaponFamily.Spear => WeaponFamily.HunterBow,
            WeaponFamily.DualDaggers => WeaponFamily.DualSwords,
            _ => family
        };
#pragma warning restore CS0618
    }

    public static string ToDisplayName(this WeaponFamily family)
    {
        return family.ToOfficialFamily() switch
        {
            WeaponFamily.LongSword => "Long Sword + Shield",
            WeaponFamily.DualSwords => "Dual Swords",
            WeaponFamily.HunterBow => "Hunter Bow",
            WeaponFamily.WarAxe => "War Axe + Buckler",
            WeaponFamily.ArcaneWarStaff => "Arcane War Staff",
            WeaponFamily.SpikedMace => "Spiked Mace",
            WeaponFamily.ChainFlail => "Chain Flail",
            _ => "Long Sword + Shield"
        };
    }
}
