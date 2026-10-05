using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns the active Goblin collection, enemy updates, and replacement spawning.
/// </summary>
public sealed class EnemyManager
{
    private const int TargetLivingCount = 3;
    private const float RespawnDelaySeconds = 1.5f;
    private const float MinimumPlayerSpawnDistance = 180f;
    private const float MinimumEnemySpawnDistance = 72f;
    private const float SpawnEdgeInset = 32f;
    private const int SpawnAttempts = 24;

    private readonly Rectangle _arenaBounds;
    private readonly List<Goblin> _goblins = new();
    private readonly Random _random;
    private float _respawnTimeRemaining = RespawnDelaySeconds;
    private int _nextEliteKillCount;
    private bool _elitePending;

    public IReadOnlyList<Goblin> Goblins => _goblins;
    public int NextEliteKillCount => _nextEliteKillCount;

    public EnemyManager(Rectangle arenaBounds, int? randomSeed = null)
    {
        _arenaBounds = arenaBounds;
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
    }

    public void Reset(Vector2 playerPosition, int playerLevel)
    {
        _goblins.Clear();
        _respawnTimeRemaining = RespawnDelaySeconds;
        _elitePending = false;
        _nextEliteKillCount = _random.Next(8, 13);

        while (_goblins.Count < TargetLivingCount)
            SpawnGoblin(playerPosition, playerLevel, isElite: false);
    }

    public void UpdateTimers(GameTime gameTime)
    {
        foreach (Goblin goblin in _goblins)
            goblin.UpdateTimers(gameTime);
    }

    public void UpdateCombatAndAi(GameTime gameTime, PlayerCharacter player)
    {
        foreach (Goblin goblin in _goblins)
        {
            if (!goblin.IsAlive || !player.IsAlive)
                continue;

            float distanceSquared = Vector2.DistanceSquared(
                goblin.Position,
                player.Position);

            if (distanceSquared <= goblin.Attack.Range * goblin.Attack.Range)
            {
                if (goblin.Attack.TryStart())
                    player.ReceiveDamage(goblin.AttackDamage);
            }
            else
            {
                goblin.UpdateMovement(
                    gameTime,
                    player.Position,
                    goblin.Attack.Range,
                    _arenaBounds);
            }
        }
    }

    public int RemoveDefeated(out int experienceReward)
    {
        int defeatedCount = 0;
        experienceReward = 0;

        for (int index = _goblins.Count - 1; index >= 0; index--)
        {
            Goblin goblin = _goblins[index];

            if (goblin.IsAlive)
                continue;

            defeatedCount++;
            experienceReward += goblin.ExperienceReward;
            _goblins.RemoveAt(index);
        }

        return defeatedCount;
    }

    public void UpdateSpawning(
        GameTime gameTime,
        Vector2 playerPosition,
        int playerLevel,
        int killCount)
    {
        UpdateEliteSchedule(killCount);

        if (_goblins.Count >= TargetLivingCount)
        {
            _respawnTimeRemaining = RespawnDelaySeconds;
            return;
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _respawnTimeRemaining -= elapsedSeconds;

        if (_respawnTimeRemaining > 0f)
            return;

        bool spawnElite = _elitePending && !HasLivingElite();
        SpawnGoblin(playerPosition, playerLevel, spawnElite);

        if (spawnElite)
            _elitePending = false;

        _respawnTimeRemaining = RespawnDelaySeconds;
    }

    private void UpdateEliteSchedule(int killCount)
    {
        while (killCount >= _nextEliteKillCount)
        {
            _elitePending = true;
            _nextEliteKillCount += _random.Next(8, 13);
        }
    }

    private bool HasLivingElite()
    {
        foreach (Goblin goblin in _goblins)
        {
            if (goblin.IsAlive && goblin.IsElite)
                return true;
        }

        return false;
    }

    private void SpawnGoblin(
        Vector2 playerPosition,
        int playerLevel,
        bool isElite)
    {
        Vector2 spawnPosition = SelectSpawnPosition(playerPosition);
        GoblinVariant variant = SelectVariant(playerLevel);
        _goblins.Add(new Goblin(
            spawnPosition,
            playerLevel,
            variant,
            isElite));
    }

    private GoblinVariant SelectVariant(int playerLevel)
    {
        int levelsAboveOne = Math.Max(0, playerLevel - 1);
        int bruteChance = Math.Min(35, 15 + levelsAboveOne * 2);
        int fastChance = Math.Min(30, 25 + levelsAboveOne);
        int roll = _random.Next(100);

        if (roll < bruteChance)
            return GoblinVariant.Brute;

        if (roll < bruteChance + fastChance)
            return GoblinVariant.Fast;

        return GoblinVariant.Normal;
    }

    private Vector2 SelectSpawnPosition(Vector2 playerPosition)
    {
        float minimumPlayerDistanceSquared =
            MinimumPlayerSpawnDistance * MinimumPlayerSpawnDistance;
        float minimumEnemyDistanceSquared =
            MinimumEnemySpawnDistance * MinimumEnemySpawnDistance;
        Vector2 bestCandidate = CreateRandomSpawnCandidate();
        float bestScore = -1f;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            Vector2 candidate = CreateRandomSpawnCandidate();
            float playerDistanceSquared = Vector2.DistanceSquared(
                candidate,
                playerPosition);
            float nearestEnemyDistanceSquared = GetNearestEnemyDistanceSquared(candidate);
            float score = playerDistanceSquared + nearestEnemyDistanceSquared * 0.5f;

            if (score > bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }

            if (playerDistanceSquared >= minimumPlayerDistanceSquared &&
                nearestEnemyDistanceSquared >= minimumEnemyDistanceSquared)
            {
                return candidate;
            }
        }

        return bestCandidate;
    }

    private float GetNearestEnemyDistanceSquared(Vector2 candidate)
    {
        if (_goblins.Count == 0)
            return MinimumEnemySpawnDistance * MinimumEnemySpawnDistance;

        float nearestDistanceSquared = float.MaxValue;

        foreach (Goblin goblin in _goblins)
        {
            float distanceSquared = Vector2.DistanceSquared(
                candidate,
                goblin.Position);
            nearestDistanceSquared = MathF.Min(
                nearestDistanceSquared,
                distanceSquared);
        }

        return nearestDistanceSquared;
    }

    private Vector2 CreateRandomSpawnCandidate()
    {
        float left = _arenaBounds.Left + SpawnEdgeInset;
        float right = _arenaBounds.Right - SpawnEdgeInset;
        float top = _arenaBounds.Top + SpawnEdgeInset;
        float bottom = _arenaBounds.Bottom - SpawnEdgeInset;
        float horizontalPosition = MathHelper.Lerp(
            left,
            right,
            (float)_random.NextDouble());
        float verticalPosition = MathHelper.Lerp(
            top,
            bottom,
            (float)_random.NextDouble());

        return _random.Next(4) switch
        {
            0 => new Vector2(horizontalPosition, top),
            1 => new Vector2(right, verticalPosition),
            2 => new Vector2(horizontalPosition, bottom),
            _ => new Vector2(left, verticalPosition)
        };
    }
}
