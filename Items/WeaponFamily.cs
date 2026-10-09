namespace DungeonAscendant.Items;

public enum WeaponFamily
{
    // Keep the original numeric values stable.  Old save payloads can still be
    // read and are normalized through WeaponFamilyExtensions.ToOfficialFamily.
    LongSword = 0,
    SpikedMace = 1,
    // Legacy serialized name retained as an alias.
    GreatSword = SpikedMace,
    WarAxe = 2,
    // Legacy serialized name retained as an alias.
    BattleAxe = WarAxe,
    // Legacy serialized value retained and normalized during load.
    Spear = 3,
    DualSwords = 4,
    // Legacy serialized name retained as an alias.
    DualDaggers = DualSwords,
    HunterBow = 5,
    // Legacy source/save name.
    Bow = HunterBow,
    ArcaneWarStaff = 6,
    // Legacy source/save name.
    ArcaneStaff = ArcaneWarStaff,
    ChainFlail = 7
}
