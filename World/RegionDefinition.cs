using DungeonAscendant.Items;

namespace DungeonAscendant.World;

public sealed class RegionDefinition
{
    public static RegionDefinition WildForest { get; } = new(
        RegionType.WildForest,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition AncientCatacombs { get; } = new(
        RegionType.AncientCatacombs,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition FallenCity { get; } = new(
        RegionType.RuinedCity,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition DemonLands { get; } = new(
        RegionType.DemonLands,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition DemonFortress { get; } = new(
        RegionType.DemonFortress,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition DemonCity { get; } = new(
        RegionType.DemonCity,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public static RegionDefinition DemonKingSanctum { get; } = new(
        RegionType.DemonKingSanctuary,
        RegionBossType.AncientTreant,
        isImplemented: true,
        bossWeaponReward: null);

    public RegionType Type { get; }
    public RegionBossType BossType { get; }
    public bool IsImplemented { get; }
    public BossWeaponReward BossWeaponReward { get; }

    private RegionDefinition(
        RegionType type,
        RegionBossType bossType,
        bool isImplemented,
        BossWeaponReward bossWeaponReward)
    {
        Type = type;
        BossType = bossType;
        IsImplemented = isImplemented;
        BossWeaponReward = bossWeaponReward;
    }

    public static RegionDefinition Get(RegionType type)
    {
        return type switch
        {
            RegionType.WildForest => WildForest,
            RegionType.AncientCatacombs => AncientCatacombs,
            RegionType.RuinedCity => FallenCity,
            RegionType.DemonLands => DemonLands,
            RegionType.DemonFortress => DemonFortress,
            RegionType.DemonCity => DemonCity,
            RegionType.DemonKingSanctuary => DemonKingSanctum,
            _ => new RegionDefinition(
                type,
                RegionBossType.AncientTreant,
                isImplemented: false,
                bossWeaponReward: null)
        };
    }
}
