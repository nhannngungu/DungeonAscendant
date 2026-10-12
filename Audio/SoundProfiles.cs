using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;

namespace DungeonAscendant.Audio;

public enum PlayerSoundEvent { Swing, AttackRelease, Hit, BlockImpact, HeavyAttack, Dash, Special }
public enum EnemySoundEvent { Idle, AttackWindup, AttackRelease, Hit, Stagger, Death, Special }
public enum ImpactMaterial { Flesh, Bone, MetalArmor, Stone, Wood, Spectral }
public enum MusicState { None, Map1Explore, Map1Combat, Map1Boss, Map2Explore, Map2Combat, Map2Boss }
public enum AmbienceState { None, Map1Forest, Map2Catacomb }
public enum UiSoundEvent { Select, Confirm, Back, InventoryOpen, NotePickup }
public enum WorldSoundEvent
{
    Map1GrassStep, Map1BranchSnap, Map1StoneStep,
    Map2StoneStep, Map2ChainRattle, Map2CoffinOpen, Map2TombRumble,
    Map2Shrine, Map2ScholarCoffin,
    Map3WindBetweenBuildings, Map3DistantFire, Map3WoodCreak,
    Map3HangingChain, Map3DistantBell, Map3Rubble,
    Map3DestructibleCollapse, Map3PalaceAmbience,
    Map4DemonicAmbience, Map4Rift, Map4Corruption,
    Map4AnchorActive, Map4AnchorDestroyed, Map4LavaFire,
    Map4DemonCombat, Map4BossPhase, Map4LegionsEnd,
    Map4AshWind, Map4DistantDemonCry, Map4BoneCreak,
    Map4QuarryChains, Map4RitualChant, Map4Campfire,
    Map4DistantWar, Map4GateChains, Map4InfernalHeart
}
public enum BossSoundEvent { Idle, Attack, Special, Death }

public sealed class CombatSoundProfile
{
    private readonly IReadOnlyDictionary<PlayerSoundEvent, string[]> _events;
    private readonly CombatSoundProfile _fallback;
    public CombatSoundProfile(IReadOnlyDictionary<PlayerSoundEvent, string[]> events, CombatSoundProfile fallback = null) { _events = events; _fallback = fallback; }
    public IReadOnlyList<string> GetClips(PlayerSoundEvent e) => _events.TryGetValue(e, out string[] c) && c.Length > 0 ? c : _fallback?.GetClips(e) ?? Array.Empty<string>();
}

public sealed class EnemySoundProfile
{
    private readonly IReadOnlyDictionary<EnemySoundEvent, string[]> _events;
    private readonly EnemySoundProfile _fallback;
    public string Id { get; }
    public ImpactMaterial ImpactMaterial { get; }
    public EnemySoundProfile(string id, ImpactMaterial material, IReadOnlyDictionary<EnemySoundEvent, string[]> events, EnemySoundProfile fallback = null) { Id=id; ImpactMaterial=material; _events=events; _fallback=fallback; }
    public IReadOnlyList<string> GetClips(EnemySoundEvent e) => _events.TryGetValue(e, out string[] c) && c.Length > 0 ? c : _fallback?.GetClips(e) ?? Array.Empty<string>();
}

public static class AudioProfiles
{
    private static string A(string path) => "Audio/" + path;
    private static string[] C(params string[] clips) => clips;

    private static readonly CombatSoundProfile PlayerFallback = new(new Dictionary<PlayerSoundEvent,string[]>
    {
        [PlayerSoundEvent.Swing]=C(A("Combat/player_swing_light_01")), [PlayerSoundEvent.AttackRelease]=C(A("Combat/player_swing_light_01")),
        [PlayerSoundEvent.Hit]=C(A("Impacts/impact_flesh_01")), [PlayerSoundEvent.BlockImpact]=C(A("Impacts/impact_metal_01")),
        [PlayerSoundEvent.HeavyAttack]=C(A("Combat/player_swing_light_01")), [PlayerSoundEvent.Dash]=C(A("Combat/player_swing_light_01")),
        [PlayerSoundEvent.Special]=C(A("Combat/player_swing_light_01"))
    });

    private static CombatSoundProfile Weapon(string folder, params string[] names) => new(new Dictionary<PlayerSoundEvent,string[]>
    {
        [PlayerSoundEvent.Swing]=C(A($"Weapons/{folder}/{names[0]}")), [PlayerSoundEvent.AttackRelease]=C(A($"Weapons/{folder}/{names[1]}")),
        [PlayerSoundEvent.Hit]=C(A($"Weapons/{folder}/{names[2]}")), [PlayerSoundEvent.BlockImpact]=C(A($"Weapons/{folder}/{names[3]}")),
        [PlayerSoundEvent.HeavyAttack]=C(A($"Weapons/{folder}/{names[4]}")), [PlayerSoundEvent.Dash]=C(A($"Weapons/{folder}/{names[5]}")),
        [PlayerSoundEvent.Special]=C(A($"Weapons/{folder}/{names[6]}"))
    }, PlayerFallback);

    private static readonly IReadOnlyDictionary<WeaponFamily,CombatSoundProfile> Players = new Dictionary<WeaponFamily,CombatSoundProfile>
    {
        [WeaponFamily.LongSword]=Weapon("Knight","knight_light_01","knight_light_01","knight_shield_bash_01","knight_block_01","knight_heavy_01","knight_light_01","knight_shield_bash_01"),
        [WeaponFamily.DualSwords]=Weapon("Duelist","duelist_slash1_01","duelist_slash2_01","duelist_slash2_01","duelist_slash1_01","duelist_flurry_01","duelist_dash_01","duelist_flurry_01"),
        [WeaponFamily.HunterBow]=Weapon("Ranger","ranger_draw_01","ranger_release_01","ranger_impact_01","ranger_impact_01","ranger_draw_01","ranger_arrow_fly_01","ranger_arrow_fly_01"),
        [WeaponFamily.WarAxe]=Weapon("Raider","raider_axe_swing_01","raider_axe_swing_01","raider_hook_01","raider_buckler_01","raider_axe_heavy_01","raider_hook_01","raider_hook_01"),
        [WeaponFamily.ArcaneWarStaff]=Weapon("Spellblade","spellblade_arcane_slash_01","spellblade_lunge_01","spellblade_detonate_01","spellblade_rune_01","spellblade_detonate_01","spellblade_lunge_01","spellblade_rune_01"),
        [WeaponFamily.SpikedMace]=Weapon("Breaker","breaker_mace_swing_01","breaker_mace_swing_01","breaker_mace_impact_01","breaker_mace_impact_01","breaker_ground_slam_01","breaker_rush_01","breaker_ground_slam_01"),
        [WeaponFamily.ChainFlail]=Weapon("Reckoner","reckoner_chain_swing_01","reckoner_chain_lash_01","reckoner_chain_lash_01","reckoner_chain_retract_01","reckoner_vortex_01","reckoner_chain_retract_01","reckoner_vortex_01")
    };

    private static EnemySoundProfile E(string id, ImpactMaterial material, string folder, params string[] n) => new(id,material,new Dictionary<EnemySoundEvent,string[]>
    {
        [EnemySoundEvent.Idle]=C(A($"Enemies/{folder}/{n[0]}")), [EnemySoundEvent.AttackWindup]=C(A($"Enemies/{folder}/{n[1]}")),
        [EnemySoundEvent.AttackRelease]=C(A($"Enemies/{folder}/{n[2]}")), [EnemySoundEvent.Hit]=C(A($"Enemies/{folder}/{n[3]}")),
        [EnemySoundEvent.Stagger]=C(A($"Enemies/{folder}/{n[4]}")), [EnemySoundEvent.Death]=C(A($"Enemies/{folder}/{n[5]}")),
        [EnemySoundEvent.Special]=C(A($"Enemies/{folder}/{n[6]}"))
    });
    private static readonly EnemySoundProfile CityHumanFallback = E(
        "city_human_fallback", ImpactMaterial.Flesh, "Map1/Goblin",
        "goblin_idle_01", "goblin_idle_01", "goblin_attack_01",
        "goblin_hurt_01", "goblin_hurt_01", "goblin_death_01",
        "goblin_attack_01");
    private static readonly EnemySoundProfile CityArmorFallback = E(
        "city_armor_fallback", ImpactMaterial.MetalArmor, "Map2/UndeadGuard",
        "undeadguard_step_01", "undeadguard_shield_01", "undeadguard_attack_01",
        "undeadguard_shield_01", "undeadguard_shield_01", "undeadguard_death_01",
        "undeadguard_shield_01");
    private static EnemySoundProfile City(
        string id,
        ImpactMaterial material,
        EnemySoundProfile fallback) =>
        new(id, material, new Dictionary<EnemySoundEvent, string[]>(), fallback);
    private static readonly EnemySoundProfile RuinedCrossbowmanSounds = new(
        "ruined_crossbowman",
        ImpactMaterial.MetalArmor,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/SkeletonArcher/skeletonarcher_draw_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/SkeletonArcher/skeletonarcher_release_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile RabidHoundSounds = new(
        "rabid_hound",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map1/DireWolf/direwolf_idle_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("World/Map2/chain_rattle_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map1/DireWolf/direwolf_attack_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map1/DireWolf/direwolf_hurt_01")),
            [EnemySoundEvent.Stagger] = C(
                A("World/Map2/chain_rattle_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map1/DireWolf/direwolf_death_01")),
            [EnemySoundEvent.Special] = C(
                A("World/Map2/chain_rattle_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile PlagueCarrierSounds = new(
        "plague_carrier",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map1/GiantSpider/giantspider_web_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Impacts/impact_wood_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map1/GiantSpider/giantspider_web_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile BellWatcherSounds = new(
        "bell_watcher",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.AttackWindup] = C(
                A("World/Map2/chain_rattle_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map1/Goblin/goblin_attack_01")),
            [EnemySoundEvent.Special] = C(
                A("World/Map2/shrine_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile StreetExecutionerSounds = new(
        "street_executioner",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.AttackWindup] = C(
                A("World/Map2/chain_rattle_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Weapons/Breaker/breaker_ground_slam_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01")),
            [EnemySoundEvent.Special] = C(
                A("Weapons/Breaker/breaker_mace_impact_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile WarBishopSounds = new(
        "war_bishop",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_whisper_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_cast_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_attack_01")),
            [EnemySoundEvent.Special] = C(
                A("World/Map2/shrine_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_death_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile BrokenKingSounds = new(
        "broken_king",
        ImpactMaterial.MetalArmor,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_phase_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_counter_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_swing_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/DeathKnight/deathknight_slam_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_final_oath_01"),
                A("World/Map2/shrine_01"))
        },
        CityArmorFallback);
    private static readonly EnemySoundProfile ImpRaiderSounds = new(
        "imp_raider",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map1/Goblin/goblin_idle_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map1/BloodBat/bloodbat_screech_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map1/Goblin/goblin_attack_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map1/Goblin/goblin_hurt_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map1/Goblin/goblin_hurt_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map1/Goblin/goblin_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Weapons/Duelist/duelist_flurry_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile HellArcherSounds = new(
        "hell_archer",
        ImpactMaterial.Flesh,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_whisper_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/SkeletonArcher/skeletonarcher_draw_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/SkeletonArcher/skeletonarcher_release_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map1/GoblinHunter/goblinhunter_hurt_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/SkeletonArcher/skeletonarcher_rattle_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map1/GoblinHunter/goblinhunter_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Weapons/Ranger/ranger_arrow_fly_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile CorruptionPriestSounds = new(
        "corruption_priest",
        ImpactMaterial.Spectral,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_whisper_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_cast_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("World/Map2/shrine_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_whisper_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_pull_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/SoulCollector/soulcollector_cast_01"),
                A("World/Map2/shrine_01"))
        },
        CityHumanFallback);
    private static readonly EnemySoundProfile RiftWardenSounds = new(
        "rift_warden",
        ImpactMaterial.MetalArmor,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/DeathKnight/deathknight_step_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/CursedKnight/cursedknight_hum_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/DeathKnight/deathknight_swing_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/DeathKnight/deathknight_slam_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/DeathKnight/deathknight_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/Wraith/wraith_phase_01"))
        },
        CityArmorFallback);
    private static readonly EnemySoundProfile DemonLegionnaireSounds = new(
        "demon_legionnaire",
        ImpactMaterial.MetalArmor,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_step_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/CursedKnight/cursedknight_hum_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/DeathKnight/deathknight_swing_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/DeathKnight/deathknight_slam_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/DeathKnight/deathknight_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01"),
                A("World/Map2/shrine_01"))
        },
        CityArmorFallback);
    private static readonly EnemySoundProfile HornedGeneralSounds = new(
        "horned_general",
        ImpactMaterial.MetalArmor,
        new Dictionary<EnemySoundEvent, string[]>
        {
            [EnemySoundEvent.Idle] = C(
                A("Enemies/Map2/DeathKnight/deathknight_step_01")),
            [EnemySoundEvent.AttackWindup] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_counter_01")),
            [EnemySoundEvent.AttackRelease] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_swing_01")),
            [EnemySoundEvent.Hit] = C(
                A("Enemies/Map2/UndeadGuard/undeadguard_shield_01")),
            [EnemySoundEvent.Stagger] = C(
                A("Enemies/Map2/DeathKnight/deathknight_slam_01")),
            [EnemySoundEvent.Death] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_death_01")),
            [EnemySoundEvent.Special] = C(
                A("Enemies/Map2/FallenKnight/fallenknight_phase_01"),
                A("Enemies/Map2/SoulCollector/soulcollector_cast_01"),
                A("World/Map2/shrine_01"),
                A("Weapons/Breaker/breaker_rush_01"),
                A("Enemies/Map2/FallenKnight/fallenknight_final_oath_01"))
        },
        CityArmorFallback);
    private static readonly IReadOnlyDictionary<EnemyType,EnemySoundProfile> Enemies = new Dictionary<EnemyType,EnemySoundProfile>
    {
        [EnemyType.DireWolf]=E("dire_wolf",ImpactMaterial.Flesh,"Map1/DireWolf","direwolf_idle_01","direwolf_idle_01","direwolf_attack_01","direwolf_hurt_01","direwolf_hurt_01","direwolf_death_01","direwolf_attack_01"),
        [EnemyType.GiantSpider]=E("giant_spider",ImpactMaterial.Flesh,"Map1/GiantSpider","giantspider_idle_01","giantspider_web_01","giantspider_attack_01","giantspider_idle_01","giantspider_idle_01","giantspider_death_01","giantspider_web_01"),
        [EnemyType.Goblin]=E("goblin",ImpactMaterial.Flesh,"Map1/Goblin","goblin_idle_01","goblin_idle_01","goblin_attack_01","goblin_hurt_01","goblin_hurt_01","goblin_death_01","goblin_attack_01"),
        [EnemyType.GoblinHunter]=E("goblin_hunter",ImpactMaterial.Flesh,"Map1/GoblinHunter","goblinhunter_idle_01","goblinhunter_bow_01","goblinhunter_bow_01","goblinhunter_hurt_01","goblinhunter_hurt_01","goblinhunter_death_01","goblinhunter_bow_01"),
        [EnemyType.ThornCrawler]=E("thorn_crawler",ImpactMaterial.Wood,"Map1/ThornCrawler","thorncrawler_rustle_01","thorncrawler_emerge_01","thorncrawler_attack_01","thorncrawler_rustle_01","thorncrawler_rustle_01","thorncrawler_death_01","thorncrawler_emerge_01"),
        [EnemyType.CorruptedTreant]=E("corrupted_treant",ImpactMaterial.Wood,"Map1/CorruptedTreant","corruptedtreant_creak_01","corruptedtreant_root_01","corruptedtreant_slam_01","corruptedtreant_creak_01","corruptedtreant_creak_01","corruptedtreant_death_01","corruptedtreant_root_01"),
        [EnemyType.BloodBat]=E("blood_bat",ImpactMaterial.Flesh,"Map1/BloodBat","bloodbat_wing_01","bloodbat_screech_01","bloodbat_attack_01","bloodbat_screech_01","bloodbat_screech_01","bloodbat_death_01","bloodbat_screech_01"),
        [EnemyType.GoblinChief]=E("goblin_chief",ImpactMaterial.Flesh,"Map1/GoblinChief","goblinchief_roar_01","goblinchief_roar_01","goblinchief_attack_01","goblinchief_hurt_01","goblinchief_hurt_01","goblinchief_death_01","goblinchief_roar_01"),
        [EnemyType.MotherSpider]=E("mother_spider",ImpactMaterial.Flesh,"Map1/MotherSpider","motherspider_screech_01","motherspider_web_01","motherspider_slam_01","motherspider_screech_01","motherspider_screech_01","motherspider_death_01","motherspider_web_01"),
        [EnemyType.Spiderling]=E("spiderling",ImpactMaterial.Flesh,"Map1/GiantSpider","giantspider_idle_01","giantspider_web_01","giantspider_attack_01","giantspider_idle_01","giantspider_idle_01","giantspider_death_01","giantspider_web_01"),
        [EnemyType.Skeleton]=E("skeleton",ImpactMaterial.Bone,"Map2/Skeleton","skeleton_rattle_01","skeleton_rattle_01","skeleton_attack_01","skeleton_hurt_01","skeleton_hurt_01","skeleton_death_01","skeleton_attack_01"),
        [EnemyType.SkeletonArcher]=E("skeleton_archer",ImpactMaterial.Bone,"Map2/SkeletonArcher","skeletonarcher_rattle_01","skeletonarcher_draw_01","skeletonarcher_release_01","skeletonarcher_rattle_01","skeletonarcher_rattle_01","skeletonarcher_death_01","skeletonarcher_draw_01"),
        [EnemyType.RottenCorpse]=E("rotten_corpse",ImpactMaterial.Flesh,"Map2/RottenCorpse","rottencorpse_groan_01","rottencorpse_groan_01","rottencorpse_vomit_01","rottencorpse_burst_01","rottencorpse_burst_01","rottencorpse_death_01","rottencorpse_burst_01"),
        [EnemyType.Wraith]=E("wraith",ImpactMaterial.Spectral,"Map2/Wraith","wraith_whisper_01","wraith_phase_01","wraith_attack_01","wraith_phase_01","wraith_phase_01","wraith_death_01","wraith_phase_01"),
        [EnemyType.UndeadGuard]=E("undead_guard",ImpactMaterial.MetalArmor,"Map2/UndeadGuard","undeadguard_step_01","undeadguard_shield_01","undeadguard_attack_01","undeadguard_shield_01","undeadguard_shield_01","undeadguard_death_01","undeadguard_shield_01"),
        [EnemyType.GraveBat]=E("grave_bat",ImpactMaterial.Flesh,"Map2/GraveBat","gravebat_wing_01","gravebat_screech_01","gravebat_attack_01","gravebat_screech_01","gravebat_screech_01","gravebat_death_01","gravebat_screech_01"),
        [EnemyType.CursedKnight]=E("cursed_knight",ImpactMaterial.MetalArmor,"Map2/CursedKnight","cursedknight_step_01","cursedknight_hum_01","cursedknight_swing_01","cursedknight_hum_01","cursedknight_hum_01","cursedknight_death_01","cursedknight_hum_01"),
        [EnemyType.SoulCollector]=E("soul_collector",ImpactMaterial.Spectral,"Map2/SoulCollector","soulcollector_whisper_01","soulcollector_cast_01","soulcollector_cast_01","soulcollector_whisper_01","soulcollector_whisper_01","soulcollector_death_01","soulcollector_pull_01"),
        [EnemyType.DeathKnight]=E("death_knight",ImpactMaterial.MetalArmor,"Map2/DeathKnight","deathknight_step_01","deathknight_step_01","deathknight_swing_01","deathknight_slam_01","deathknight_slam_01","deathknight_death_01","deathknight_slam_01"),
        [EnemyType.FallenKnight]=E("fallen_knight",ImpactMaterial.MetalArmor,"Map2/FallenKnight","fallenknight_phase_01","fallenknight_counter_01","fallenknight_swing_01","fallenknight_counter_01","fallenknight_counter_01","fallenknight_death_01","fallenknight_final_oath_01"),
        [EnemyType.FallenMilitiaman]=City("fallen_militiaman",ImpactMaterial.Flesh,CityHumanFallback),
        [EnemyType.RuinedCrossbowman]=RuinedCrossbowmanSounds,
        [EnemyType.RabidHound]=RabidHoundSounds,
        [EnemyType.PlagueCarrier]=PlagueCarrierSounds,
        [EnemyType.BellWatcher]=BellWatcherSounds,
        [EnemyType.StreetExecutioner]=StreetExecutionerSounds,
        [EnemyType.RoyalGuard]=City("royal_guard",ImpactMaterial.MetalArmor,CityArmorFallback),
        [EnemyType.AshKnight]=City("ash_knight",ImpactMaterial.MetalArmor,CityArmorFallback),
        [EnemyType.WarBishop]=WarBishopSounds,
        [EnemyType.BrokenKing]=BrokenKingSounds,
        [EnemyType.ImpRaider]=ImpRaiderSounds,
        [EnemyType.HellArcher]=HellArcherSounds,
        [EnemyType.SpineHound]=City("spine_hound",ImpactMaterial.Flesh,CityHumanFallback),
        [EnemyType.RiftStalker]=City("rift_stalker",ImpactMaterial.Spectral,CityHumanFallback),
        [EnemyType.BoneHarvester]=City("bone_harvester",ImpactMaterial.Bone,CityArmorFallback),
        [EnemyType.Hellbrute]=City("hellbrute",ImpactMaterial.Flesh,CityArmorFallback),
        [EnemyType.CorruptionPriest]=CorruptionPriestSounds,
        [EnemyType.RiftWarden]=RiftWardenSounds,
        [EnemyType.DemonLegionnaire]=DemonLegionnaireSounds,
        [EnemyType.HornedGeneral]=HornedGeneralSounds
    };

    private static readonly IReadOnlyDictionary<ImpactMaterial,string[]> Impacts = new Dictionary<ImpactMaterial,string[]>
    {
        [ImpactMaterial.Bone]=I("bone"), [ImpactMaterial.Flesh]=I("flesh"), [ImpactMaterial.MetalArmor]=I("metal"),
        [ImpactMaterial.Stone]=I("stone"), [ImpactMaterial.Wood]=I("wood"), [ImpactMaterial.Spectral]=I("spectral")
    };
    private static string[] I(string material) => C(A($"Impacts/impact_{material}_01"),A($"Impacts/impact_{material}_02"),A($"Impacts/impact_{material}_03"));
    private static readonly IReadOnlyDictionary<MusicState,string> Music = new Dictionary<MusicState,string> { [MusicState.Map1Explore]=A("Music/Map1/wilderness_explore_epic"),[MusicState.Map1Combat]=A("Music/Map1/wilderness_combat_epic"),[MusicState.Map1Boss]=A("Music/Map1/ancient_treant_boss_epic"),[MusicState.Map2Explore]=A("Music/Map2/catacombs_explore_epic"),[MusicState.Map2Combat]=A("Music/Map2/catacombs_combat_epic"),[MusicState.Map2Boss]=A("Music/Map2/fallen_knight_boss_epic") };
    private static readonly IReadOnlyDictionary<AmbienceState,string> Ambience = new Dictionary<AmbienceState,string> { [AmbienceState.Map1Forest]=A("Ambience/map1_forest"),[AmbienceState.Map2Catacomb]=A("Ambience/map2_catacomb") };
    private static readonly IReadOnlyDictionary<UiSoundEvent,string[]> Ui = new Dictionary<UiSoundEvent,string[]> { [UiSoundEvent.Select]=C(A("UI/ui_select_01")),[UiSoundEvent.Confirm]=C(A("UI/ui_confirm_01")),[UiSoundEvent.Back]=C(A("UI/ui_back_01")),[UiSoundEvent.InventoryOpen]=C(A("UI/ui_inventory_open_01")),[UiSoundEvent.NotePickup]=C(A("UI/ui_note_pickup_01")) };
    private static readonly IReadOnlyDictionary<WorldSoundEvent,string[]> World = new Dictionary<WorldSoundEvent,string[]>
    {
        [WorldSoundEvent.Map1GrassStep]=C(A("World/Map1/grass_step_01")),[WorldSoundEvent.Map1BranchSnap]=C(A("World/Map1/branch_snap_01")),[WorldSoundEvent.Map1StoneStep]=C(A("World/Map1/stone_step_01")),
        [WorldSoundEvent.Map2StoneStep]=C(A("World/Map2/stone_step_01")),[WorldSoundEvent.Map2ChainRattle]=C(A("World/Map2/chain_rattle_01")),[WorldSoundEvent.Map2CoffinOpen]=C(A("World/Map2/coffin_open_01")),[WorldSoundEvent.Map2TombRumble]=C(A("World/Map2/tomb_rumble_01")),[WorldSoundEvent.Map2Shrine]=C(A("World/Map2/shrine_01")),[WorldSoundEvent.Map2ScholarCoffin]=C(A("World/Map2/scholar_coffin_01")),
        // Semantic Map 3 hooks. Empty clip lists deliberately keep these as
        // no-op placeholders until a dedicated Fallen City audio pack exists.
        [WorldSoundEvent.Map3WindBetweenBuildings]=Array.Empty<string>(),
        [WorldSoundEvent.Map3DistantFire]=Array.Empty<string>(),
        [WorldSoundEvent.Map3WoodCreak]=Array.Empty<string>(),
        [WorldSoundEvent.Map3HangingChain]=Array.Empty<string>(),
        [WorldSoundEvent.Map3DistantBell]=Array.Empty<string>(),
        [WorldSoundEvent.Map3Rubble]=Array.Empty<string>(),
        [WorldSoundEvent.Map3DestructibleCollapse]=Array.Empty<string>(),
        [WorldSoundEvent.Map3PalaceAmbience]=Array.Empty<string>(),
        // Semantic Map 4 hooks. These intentionally remain silent until the
        // dedicated Demon Lands audio assets are produced.
        [WorldSoundEvent.Map4DemonicAmbience]=Array.Empty<string>(),
        [WorldSoundEvent.Map4Rift]=Array.Empty<string>(),
        [WorldSoundEvent.Map4Corruption]=Array.Empty<string>(),
        [WorldSoundEvent.Map4AnchorActive]=Array.Empty<string>(),
        [WorldSoundEvent.Map4AnchorDestroyed]=Array.Empty<string>(),
        [WorldSoundEvent.Map4LavaFire]=Array.Empty<string>(),
        [WorldSoundEvent.Map4DemonCombat]=Array.Empty<string>(),
        [WorldSoundEvent.Map4BossPhase]=Array.Empty<string>(),
        [WorldSoundEvent.Map4LegionsEnd]=Array.Empty<string>(),
        [WorldSoundEvent.Map4AshWind]=Array.Empty<string>(),
        [WorldSoundEvent.Map4DistantDemonCry]=Array.Empty<string>(),
        [WorldSoundEvent.Map4BoneCreak]=Array.Empty<string>(),
        [WorldSoundEvent.Map4QuarryChains]=Array.Empty<string>(),
        [WorldSoundEvent.Map4RitualChant]=Array.Empty<string>(),
        [WorldSoundEvent.Map4Campfire]=Array.Empty<string>(),
        [WorldSoundEvent.Map4DistantWar]=Array.Empty<string>(),
        [WorldSoundEvent.Map4GateChains]=Array.Empty<string>(),
        [WorldSoundEvent.Map4InfernalHeart]=Array.Empty<string>()
    };
    private static readonly IReadOnlyDictionary<BossSoundEvent,string[]> Boss = new Dictionary<BossSoundEvent,string[]> { [BossSoundEvent.Idle]=C(A("Enemies/Map1/AncientTreant/ancienttreant_groan_01")),[BossSoundEvent.Attack]=C(A("Enemies/Map1/AncientTreant/ancienttreant_slam_01")),[BossSoundEvent.Special]=C(A("Enemies/Map1/AncientTreant/ancienttreant_roar_01")),[BossSoundEvent.Death]=C(A("Enemies/Map1/AncientTreant/ancienttreant_death_01")) };

    public static CombatSoundProfile GetPlayer(WeaponFamily f) => Players.TryGetValue(f.ToOfficialFamily(),out CombatSoundProfile p)?p:PlayerFallback;
    public static EnemySoundProfile GetEnemy(EnemyType t) => Enemies[t];
    public static IReadOnlyList<string> GetImpact(ImpactMaterial m)=>Impacts[m];
    public static string GetMusic(MusicState s)=>Music.TryGetValue(s,out string a)?a:null;
    public static string GetAmbience(AmbienceState s)=>Ambience.TryGetValue(s,out string a)?a:null;
    public static IReadOnlyList<string> GetUi(UiSoundEvent e)=>Ui[e];
    public static IReadOnlyList<string> GetWorld(WorldSoundEvent e)=>World[e];
    public static IReadOnlyList<string> GetBoss(BossSoundEvent e)=>Boss[e];
    public static IEnumerable<string> EnumerateAssets()
    {
        var found=new HashSet<string>(StringComparer.Ordinal);
        foreach(WeaponFamily f in Enum.GetValues<WeaponFamily>()) foreach(PlayerSoundEvent e in Enum.GetValues<PlayerSoundEvent>()) foreach(string a in GetPlayer(f).GetClips(e)) if(found.Add(a)) yield return a;
        foreach(EnemyType t in Enum.GetValues<EnemyType>()) foreach(EnemySoundEvent e in Enum.GetValues<EnemySoundEvent>()) foreach(string a in GetEnemy(t).GetClips(e)) if(found.Add(a)) yield return a;
        foreach(ImpactMaterial m in Enum.GetValues<ImpactMaterial>()) foreach(string a in GetImpact(m)) if(found.Add(a)) yield return a;
        foreach(MusicState s in Enum.GetValues<MusicState>()){string a=GetMusic(s);if(a!=null&&found.Add(a))yield return a;}
        foreach(AmbienceState s in Enum.GetValues<AmbienceState>()){string a=GetAmbience(s);if(a!=null&&found.Add(a))yield return a;}
        foreach(UiSoundEvent e in Enum.GetValues<UiSoundEvent>())foreach(string a in GetUi(e))if(found.Add(a))yield return a;
        foreach(WorldSoundEvent e in Enum.GetValues<WorldSoundEvent>())foreach(string a in GetWorld(e))if(found.Add(a))yield return a;
        foreach(BossSoundEvent e in Enum.GetValues<BossSoundEvent>())foreach(string a in GetBoss(e))if(found.Add(a))yield return a;
    }
}
