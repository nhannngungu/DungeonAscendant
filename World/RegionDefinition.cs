using DungeonAscendant.Items;

namespace DungeonAscendant.World;

public sealed class RegionDefinition
{
    public static RegionDefinition WildForest { get; } = new(
        RegionType.WildForest,
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
        return type == RegionType.WildForest
            ? WildForest
            : new RegionDefinition(
                type,
                RegionBossType.AncientTreant,
                isImplemented: false,
                bossWeaponReward: null);
    }
}
