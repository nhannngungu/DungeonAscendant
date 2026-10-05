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
    private const float MinimumSpawnDistance = 180f;

    private readonly Rectangle _arenaBounds;
    private readonly List<Goblin> _goblins = new();
    private readonly Vector2[] _spawnPoints;
    private float _respawnTimeRemaining = RespawnDelaySeconds;
    private int _nextSpawnPointIndex;

    public IReadOnlyList<Goblin> Goblins => _goblins;

    public EnemyManager(Rectangle arenaBounds)
    {
        _arenaBounds = arenaBounds;
        _spawnPoints = CreateSpawnPoints(arenaBounds);
    }

    public void Reset(Vector2 playerPosition, int playerLevel)
    {
        _goblins.Clear();
        _nextSpawnPointIndex = 0;
        _respawnTimeRemaining = RespawnDelaySeconds;

        while (_goblins.Count < TargetLivingCount)
            SpawnGoblin(playerPosition, playerLevel);
    }

    public void UpdateCombatAndAi(GameTime gameTime, PlayerCharacter player)
    {
        foreach (Goblin goblin in _goblins)
        {
            goblin.Attack.Update(gameTime);

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
                goblin.Update(
                    gameTime,
                    player.Position,
                    goblin.Attack.Range,
                    _arenaBounds);
            }
        }
    }

    public Goblin FindNearestTarget(Vector2 position, float range)
    {
        Goblin nearest = null;
        float nearestDistanceSquared = range * range;

        foreach (Goblin goblin in _goblins)
        {
            if (!goblin.IsAlive)
                continue;

            float distanceSquared = Vector2.DistanceSquared(
                position,
                goblin.Position);

            if (distanceSquared <= nearestDistanceSquared)
            {
                nearest = goblin;
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearest;
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
        int playerLevel)
    {
        if (_goblins.Count >= TargetLivingCount)
        {
            _respawnTimeRemaining = RespawnDelaySeconds;
            return;
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _respawnTimeRemaining -= elapsedSeconds;

        if (_respawnTimeRemaining > 0f)
            return;

        SpawnGoblin(playerPosition, playerLevel);
        _respawnTimeRemaining = RespawnDelaySeconds;
    }

    private void SpawnGoblin(Vector2 playerPosition, int playerLevel)
    {
        Vector2 spawnPosition = SelectSpawnPosition(playerPosition);
        _goblins.Add(new Goblin(spawnPosition, playerLevel));
    }

    private Vector2 SelectSpawnPosition(Vector2 playerPosition)
    {
        float minimumDistanceSquared = MinimumSpawnDistance * MinimumSpawnDistance;
        Vector2 farthestPoint = _spawnPoints[0];
        float farthestDistanceSquared = -1f;
        int farthestIndex = 0;

        for (int offset = 0; offset < _spawnPoints.Length; offset++)
        {
            int index = (_nextSpawnPointIndex + offset) % _spawnPoints.Length;
            Vector2 spawnPoint = _spawnPoints[index];
            float distanceSquared = Vector2.DistanceSquared(
                spawnPoint,
                playerPosition);

            if (distanceSquared > farthestDistanceSquared)
            {
                farthestPoint = spawnPoint;
                farthestDistanceSquared = distanceSquared;
                farthestIndex = index;
            }

            if (distanceSquared < minimumDistanceSquared)
                continue;

            _nextSpawnPointIndex = (index + 1) % _spawnPoints.Length;
            return spawnPoint;
        }

        _nextSpawnPointIndex = (farthestIndex + 1) % _spawnPoints.Length;
        return farthestPoint;
    }

    private static Vector2[] CreateSpawnPoints(Rectangle arenaBounds)
    {
        const float edgeInset = 40f;

        float left = arenaBounds.Left + edgeInset;
        float right = arenaBounds.Right - edgeInset;
        float top = arenaBounds.Top + edgeInset;
        float bottom = arenaBounds.Bottom - edgeInset;
        float centerX = arenaBounds.Center.X;
        float centerY = arenaBounds.Center.Y;

        return new[]
        {
            new Vector2(left, top),
            new Vector2(right, top),
            new Vector2(right, bottom),
            new Vector2(left, bottom),
            new Vector2(centerX, top),
            new Vector2(right, centerY),
            new Vector2(centerX, bottom),
            new Vector2(left, centerY)
        };
    }
}
