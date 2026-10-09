using System;
using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Enemies;
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
    AncientTreant
}

public sealed class EnemyIntroductionDefinition
{
    public WildForestEnemyIdentity Identity { get; }
    public string Name { get; }
    public string Title { get; }
    public string Description { get; }
    public int PresentationStrength { get; }

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
        new(WildForestEnemyIdentity.AncientTreant, "ANCIENT TREANT", "GUARDIAN OF THE FORGOTTEN FOREST", "THE SANCTUARYS LAST KEEPER HAS AWAKENED", 3)
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
