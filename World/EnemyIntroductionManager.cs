using System;
using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Enemies;
using DungeonAscendant.Localization;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum WildForestEnemyIdentity
{
    DireWolf,
    GiantSpider,
    Goblin,
    GoblinHunter,
    ThornCrawler,
    CorruptedTreant,
    BloodBat,
    GoblinChief,
    MotherSpider,
    AncientTreant,
    Skeleton, SkeletonArcher, RottenCorpse, Wraith, UndeadGuard,
    CursedKnight, GraveBat, DeathKnight, SoulCollector, FallenKnight,
    FallenMilitiaman, RuinedCrossbowman, RabidHound, PlagueCarrier,
    BellWatcher, StreetExecutioner, RoyalGuard, AshKnight, WarBishop,
    BrokenKing
}

public sealed class EnemyIntroductionDefinition
{
    public WildForestEnemyIdentity Identity { get; }
    public string Name { get; }
    public string Title { get; }
    public string Description { get; }
    public string LocalizedName => Loc.Get(LocalizationKey + ".name");
    public string LocalizedTitle => Loc.Get(LocalizationKey + ".title");
    public string LocalizedDescription => Loc.Get(LocalizationKey + ".description");
    public int PresentationStrength { get; }

    private string LocalizationKey => "enemy." + Identity switch
    {
        WildForestEnemyIdentity.DireWolf => "dire_wolf",
        WildForestEnemyIdentity.GiantSpider => "giant_spider",
        WildForestEnemyIdentity.Goblin => "goblin",
        WildForestEnemyIdentity.GoblinHunter => "goblin_hunter",
        WildForestEnemyIdentity.ThornCrawler => "thorn_crawler",
        WildForestEnemyIdentity.CorruptedTreant => "corrupted_treant",
        WildForestEnemyIdentity.BloodBat => "blood_bat",
        WildForestEnemyIdentity.GoblinChief => "goblin_chief",
        WildForestEnemyIdentity.MotherSpider => "mother_spider",
        WildForestEnemyIdentity.AncientTreant => "ancient_treant",
        WildForestEnemyIdentity.Skeleton => "skeleton",
        WildForestEnemyIdentity.SkeletonArcher => "skeleton_archer",
        WildForestEnemyIdentity.RottenCorpse => "rotten_corpse",
        WildForestEnemyIdentity.Wraith => "wraith",
        WildForestEnemyIdentity.UndeadGuard => "undead_guard",
        WildForestEnemyIdentity.CursedKnight => "cursed_knight",
        WildForestEnemyIdentity.GraveBat => "grave_bat",
        WildForestEnemyIdentity.DeathKnight => "death_knight",
        WildForestEnemyIdentity.SoulCollector => "soul_collector",
        WildForestEnemyIdentity.FallenKnight => "fallen_knight",
        WildForestEnemyIdentity.FallenMilitiaman => "fallen_militiaman",
        WildForestEnemyIdentity.RuinedCrossbowman => "ruined_crossbowman",
        WildForestEnemyIdentity.RabidHound => "rabid_hound",
        WildForestEnemyIdentity.PlagueCarrier => "plague_carrier",
        WildForestEnemyIdentity.BellWatcher => "bell_watcher",
        WildForestEnemyIdentity.StreetExecutioner => "street_executioner",
        WildForestEnemyIdentity.RoyalGuard => "royal_guard",
        WildForestEnemyIdentity.AshKnight => "ash_knight",
        WildForestEnemyIdentity.WarBishop => "war_bishop",
        WildForestEnemyIdentity.BrokenKing => "broken_king",
        _ => "unknown"
    };

    public EnemyIntroductionDefinition(
        WildForestEnemyIdentity identity,
        string name,
        string title,
        string description,
        int presentationStrength)
    {
        Identity = identity;
        Name = name;
        Title = title;
        Description = description;
        PresentationStrength = Math.Clamp(presentationStrength, 1, 3);
    }
}

/// <summary>Run-local enemy discovery and safe first-sighting presentation.</summary>
public sealed class EnemyIntroductionManager
{
    public const float RecognitionDistance = 560f;

    private static readonly EnemyIntroductionDefinition[] Definitions =
    {
        new(WildForestEnemyIdentity.DireWolf, "DIRE WOLF", "THE CORRUPTED PREDATOR", "A SAVAGE HUNTER TWISTED BY ANCIENT CORRUPTION", 1),
        new(WildForestEnemyIdentity.GiantSpider, "GIANT SPIDER", "HUNTER OF THE WEBWOOD", "IT WAITS WHERE SILK AND SHADOW CLOSE THE PATH", 1),
        new(WildForestEnemyIdentity.Goblin, "GOBLIN", "SCAVENGER OF THE WILDS", "A CRUEL RAIDER MADE BOLD BY THE DYING FOREST", 1),
        new(WildForestEnemyIdentity.GoblinHunter, "GOBLIN HUNTER", "ARROW IN THE SHADOWS", "A PATIENT MARKSMAN WHO CLAIMS THE DISTANT PATH", 1),
        new(WildForestEnemyIdentity.ThornCrawler, "THORN CRAWLER", "THE BURIED THORN", "CORRUPTED ROOT AND HUNGER STIR BENEATH THE SOIL", 1),
        new(WildForestEnemyIdentity.CorruptedTreant, "CORRUPTED TREANT", "THE ROTTING GUARDIAN", "AN ANCIENT WARDEN HOLLOWED BY LIVING ROT", 1),
        new(WildForestEnemyIdentity.BloodBat, "BLOOD BAT", "THE CRIMSON WING", "A SWIFT SCAVENGER DRAWN TO WARM BLOOD", 1),
        new(WildForestEnemyIdentity.GoblinChief, "GOBLIN CHIEF", "WARLORD OF THE WILDS", "THE WAR CAMP BENDS TO HIS BRUTAL COMMAND", 2),
        new(WildForestEnemyIdentity.MotherSpider, "MOTHER SPIDER", "BROODMOTHER OF WEBWOOD", "HER BROOD FEEDS WHERE THE FOREST CANNOT SEE", 2),
        new(WildForestEnemyIdentity.AncientTreant, "ANCIENT TREANT", "GUARDIAN OF THE FORGOTTEN FOREST", "THE SANCTUARYS LAST KEEPER HAS AWAKENED", 3),
        new(WildForestEnemyIdentity.Skeleton,"SKELETON","THE RESTLESS DEAD","ONLY BONES REMAIN YET THE CURSE STILL COMMANDS THEM",1),
        new(WildForestEnemyIdentity.SkeletonArcher,"SKELETON ARCHER","THE SILENT WATCHER","EVEN DEATH COULD NOT END ITS VIGIL",1),
        new(WildForestEnemyIdentity.RottenCorpse,"ROTTEN CORPSE","BEARER OF GRAVE ROT","THE CURSE FESTERS INSIDE FLESH THAT NO LONGER BREATHES",1),
        new(WildForestEnemyIdentity.Wraith,"WRAITH","ECHO OF THE UNBURIED","A SOUL THAT HAS FORGOTTEN BOTH LIFE AND DEATH",1),
        new(WildForestEnemyIdentity.UndeadGuard,"UNDEAD GUARD","KEEPER OF THE DEAD","EVEN IN DEATH IT STILL GUARDS WHAT LIES BEYOND",1),
        new(WildForestEnemyIdentity.CursedKnight,"CURSED KNIGHT","THE FORSAKEN OATH","HONOR DIED LONG AGO THE OATH DID NOT",2),
        new(WildForestEnemyIdentity.GraveBat,"GRAVE BAT","WHISPER OF THE CRYPT","ITS CRY DRAINS THE STRENGTH FROM THOSE WHO STILL BREATHE",1),
        new(WildForestEnemyIdentity.DeathKnight,"DEATH KNIGHT","GUARDIAN OF THE WAR TOMB","DEATH ENDED THE BATTLE. IT DID NOT END HIS DUTY.",2),
        new(WildForestEnemyIdentity.SoulCollector,"SOUL COLLECTOR","WARDEN OF LOST SOULS","IT GATHERS THE DEAD SO NONE MAY EVER LEAVE",2),
        new(WildForestEnemyIdentity.FallenKnight,"THE FALLEN KNIGHT","THE LAST OATH OF THE CATACOMBS","ONCE A GUARDIAN OF THE DEAD. NOW THEIR FINAL PRISONER.",3),
        new(WildForestEnemyIdentity.FallenMilitiaman,"FALLEN MILITIAMAN","THE SOLDIER WHO NEVER RETURNED","A common defender still fighting a war already lost.",1),
        new(WildForestEnemyIdentity.RuinedCrossbowman,"RUINED CROSSBOWMAN","WATCHER OF EMPTY STREETS","A patient marksman entrenched in shattered masonry.",1),
        new(WildForestEnemyIdentity.RabidHound,"RABID HOUND","HOUND OF THE FALLEN CITY","War left it only hunger and fear.",1),
        new(WildForestEnemyIdentity.PlagueCarrier,"PLAGUE CARRIER","BEARER OF THE BLACK BREATH","The city disease follows wherever it falls.",1),
        new(WildForestEnemyIdentity.BellWatcher,"BELL WATCHER","VOICE OF THE ALARM","Kill the watcher before the city answers.",1),
        new(WildForestEnemyIdentity.StreetExecutioner,"STREET EXECUTIONER","THE LAST SENTENCE","The square still remembers his axe.",2),
        new(WildForestEnemyIdentity.RoyalGuard,"ROYAL GUARD","SHIELD OF A DEAD KINGDOM","A final formation guards a crown already lost.",2),
        new(WildForestEnemyIdentity.AshKnight,"ASH KNIGHT","KNIGHT OF THE BURNING QUARTER","Embers still breathe beneath the blackened plate.",2),
        new(WildForestEnemyIdentity.WarBishop,"WAR BISHOP","PREACHER OF THE FINAL WAR","His last sermon turns nearby survivors into soldiers.",2),
        new(WildForestEnemyIdentity.BrokenKing,"THE BROKEN KING","LAST SOVEREIGN OF THE FALLEN CITY","A wounded king still holds the ruined throne plaza.",3)
    };

    private readonly HashSet<WildForestEnemyIdentity> _discovered = new();

    public EnemyIntroductionDefinition Active { get; private set; }
    public float TimeRemaining { get; private set; }
    public float Duration { get; private set; }
    public bool IsPresenting => Active != null && TimeRemaining > 0f;
    public bool JustTriggered { get; private set; }
    public int DiscoveryCount => _discovered.Count;
    public event Action<WildForestEnemyIdentity> EnemyDiscovered;

    public void Reset()
    {
        _discovered.Clear();
        Active = null;
        TimeRemaining = 0f;
        Duration = 0f;
        JustTriggered = false;
    }

    public void Update(
        GameTime gameTime,
        Vector2 playerPosition,
        IReadOnlyList<Enemy> enemies,
        AncientTreant boss,
        bool allowBossIntroduction)
    {
        JustTriggered = false;
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        if (IsPresenting)
        {
            TimeRemaining = MathF.Max(0f, TimeRemaining - elapsed);
            if (TimeRemaining <= 0f)
                Active = null;
            return;
        }

        float limitSquared = RecognitionDistance * RecognitionDistance;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive || enemy.Type == EnemyType.Spiderling ||
                Vector2.DistanceSquared(playerPosition, enemy.Position) > limitSquared)
                continue;
            WildForestEnemyIdentity identity = ToIdentity(enemy.Type);
            if (!_discovered.Contains(identity))
            {
                Begin(identity);
                return;
            }
        }

        if (allowBossIntroduction && boss != null && boss.IsAlive &&
            Vector2.DistanceSquared(playerPosition, boss.Position) <= limitSquared &&
            !_discovered.Contains(WildForestEnemyIdentity.AncientTreant))
            Begin(WildForestEnemyIdentity.AncientTreant);
    }

    public bool HasDiscovered(WildForestEnemyIdentity identity) =>
        _discovered.Contains(identity);

    private void Begin(WildForestEnemyIdentity identity)
    {
        Active = FindDefinition(identity);
        _discovered.Add(identity);
        Duration = Active.PresentationStrength switch
        {
            3 => 3.15f,
            2 => 2.65f,
            _ => 2.15f
        };
        TimeRemaining = Duration;
        JustTriggered = true;
        EnemyDiscovered?.Invoke(identity);
    }

    private static EnemyIntroductionDefinition FindDefinition(
        WildForestEnemyIdentity identity)
    {
        foreach (EnemyIntroductionDefinition definition in Definitions)
            if (definition.Identity == identity)
                return definition;
        throw new ArgumentOutOfRangeException(nameof(identity));
    }

    private static WildForestEnemyIdentity ToIdentity(EnemyType type) =>
        type switch
        {
            EnemyType.DireWolf => WildForestEnemyIdentity.DireWolf,
            EnemyType.GiantSpider => WildForestEnemyIdentity.GiantSpider,
            EnemyType.Goblin => WildForestEnemyIdentity.Goblin,
            EnemyType.GoblinHunter => WildForestEnemyIdentity.GoblinHunter,
            EnemyType.ThornCrawler => WildForestEnemyIdentity.ThornCrawler,
            EnemyType.CorruptedTreant => WildForestEnemyIdentity.CorruptedTreant,
            EnemyType.BloodBat => WildForestEnemyIdentity.BloodBat,
            EnemyType.GoblinChief => WildForestEnemyIdentity.GoblinChief,
            EnemyType.MotherSpider => WildForestEnemyIdentity.MotherSpider,
            EnemyType.Skeleton => WildForestEnemyIdentity.Skeleton,
            EnemyType.SkeletonArcher => WildForestEnemyIdentity.SkeletonArcher,
            EnemyType.RottenCorpse => WildForestEnemyIdentity.RottenCorpse,
            EnemyType.Wraith => WildForestEnemyIdentity.Wraith,
            EnemyType.UndeadGuard => WildForestEnemyIdentity.UndeadGuard,
            EnemyType.CursedKnight => WildForestEnemyIdentity.CursedKnight,
            EnemyType.GraveBat => WildForestEnemyIdentity.GraveBat,
            EnemyType.DeathKnight => WildForestEnemyIdentity.DeathKnight,
            EnemyType.SoulCollector => WildForestEnemyIdentity.SoulCollector,
            EnemyType.FallenKnight => WildForestEnemyIdentity.FallenKnight,
            EnemyType.FallenMilitiaman => WildForestEnemyIdentity.FallenMilitiaman,
            EnemyType.RuinedCrossbowman => WildForestEnemyIdentity.RuinedCrossbowman,
            EnemyType.RabidHound => WildForestEnemyIdentity.RabidHound,
            EnemyType.PlagueCarrier => WildForestEnemyIdentity.PlagueCarrier,
            EnemyType.BellWatcher => WildForestEnemyIdentity.BellWatcher,
            EnemyType.StreetExecutioner => WildForestEnemyIdentity.StreetExecutioner,
            EnemyType.RoyalGuard => WildForestEnemyIdentity.RoyalGuard,
            EnemyType.AshKnight => WildForestEnemyIdentity.AshKnight,
            EnemyType.WarBishop => WildForestEnemyIdentity.WarBishop,
            EnemyType.BrokenKing => WildForestEnemyIdentity.BrokenKing,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
