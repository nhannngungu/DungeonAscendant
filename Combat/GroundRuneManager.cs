using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class GroundRuneManager
{
    private readonly List<GroundRune> _runes = new();
    private readonly List<RuneDetonationVisual> _detonationVisuals = new();
    private int _nextId;
    private float _networkTimeRemaining;
    private float _networkTickClock;

    public IReadOnlyList<GroundRune> Runes => _runes;
    public IReadOnlyList<RuneDetonationVisual> DetonationVisuals =>
        _detonationVisuals;
    public int Count => _runes.Count;
    public bool IsNetworkActive => _networkTimeRemaining > 0f;
    public bool NetworkTickPending { get; private set; }
    public bool NetworkDetonationPending { get; private set; }

    public int CountWithin(Vector2 center, float radius)
    {
        int count = 0;
        float radiusSquared = radius * radius;
        foreach (GroundRune rune in _runes)
        {
            if (Vector2.DistanceSquared(center, rune.Position) <= radiusSquared)
                count++;
        }
        return count;
    }

    public bool TryPlace(
        Vector2 desiredPosition,
        int roomId,
        DungeonMap dungeon)
    {
        if (!TrySnapToGround(desiredPosition, roomId, dungeon, out Vector2 position))
            return false;

        if (_runes.Count >= SpellbladeTuning.MaximumGroundRunes)
            _runes.RemoveAt(0);
        _runes.Add(new GroundRune(++_nextId, position, roomId));
        return true;
    }

    public bool StartNetwork(Vector2 center)
    {
        if (_runes.Count < 2)
            return false;

        int eligible = 0;
        foreach (GroundRune rune in _runes)
        {
            if (Vector2.DistanceSquared(center, rune.Position) <=
                SpellbladeTuning.ConvergenceMaximumRange *
                SpellbladeTuning.ConvergenceMaximumRange)
            {
                rune.State = GroundRuneState.Network;
                eligible++;
            }
        }

        if (eligible < 2)
        {
            foreach (GroundRune rune in _runes)
                rune.State = GroundRuneState.Active;
            return false;
        }

        _networkTimeRemaining = SpellbladeTuning.ConvergenceDurationSeconds;
        _networkTickClock = 0f;
        return true;
    }

    public void AwakenAll(Vector2 center, float radius)
    {
        foreach (GroundRune rune in _runes)
        {
            if (Vector2.DistanceSquared(center, rune.Position) <= radius * radius)
                rune.State = GroundRuneState.Awakened;
        }
    }

    public List<Vector2> DetonateWithin(Vector2 center, float radius)
    {
        var positions = new List<Vector2>();
        for (int index = _runes.Count - 1; index >= 0; index--)
        {
            GroundRune rune = _runes[index];
            if (Vector2.DistanceSquared(center, rune.Position) > radius * radius)
                continue;
            positions.Add(rune.Position);
            _detonationVisuals.Add(new RuneDetonationVisual(rune.Position));
            _runes.RemoveAt(index);
        }
        ResetNetworkIfEmpty();
        return positions;
    }

    public List<Vector2> ConsumeNetworkRunes()
    {
        var positions = new List<Vector2>();
        for (int index = _runes.Count - 1; index >= 0; index--)
        {
            if (_runes[index].State != GroundRuneState.Network)
                continue;
            positions.Add(_runes[index].Position);
            _detonationVisuals.Add(
                new RuneDetonationVisual(_runes[index].Position));
            _runes.RemoveAt(index);
        }
        _networkTimeRemaining = 0f;
        NetworkDetonationPending = false;
        return positions;
    }

    public void Update(float elapsedSeconds)
    {
        elapsedSeconds = MathF.Max(0f, elapsedSeconds);
        NetworkTickPending = false;
        NetworkDetonationPending = false;

        for (int index = _detonationVisuals.Count - 1; index >= 0; index--)
        {
            _detonationVisuals[index].TimeRemaining -= elapsedSeconds;
            if (_detonationVisuals[index].TimeRemaining <= 0f)
                _detonationVisuals.RemoveAt(index);
        }

        for (int index = _runes.Count - 1; index >= 0; index--)
        {
            if (_runes[index].State == GroundRuneState.Network)
                continue;
            _runes[index].LifetimeRemaining -= elapsedSeconds;
            if (_runes[index].LifetimeRemaining <= 0f)
                _runes.RemoveAt(index);
        }

        if (_networkTimeRemaining <= 0f)
            return;
        _networkTimeRemaining = MathF.Max(0f, _networkTimeRemaining - elapsedSeconds);
        _networkTickClock += elapsedSeconds;
        if (_networkTickClock >= SpellbladeTuning.ConvergenceTickIntervalSeconds)
        {
            _networkTickClock -= SpellbladeTuning.ConvergenceTickIntervalSeconds;
            NetworkTickPending = true;
        }
        if (_networkTimeRemaining <= 0f)
            NetworkDetonationPending = true;
    }

    public void Clear()
    {
        _runes.Clear();
        _detonationVisuals.Clear();
        _networkTimeRemaining = 0f;
        _networkTickClock = 0f;
        NetworkTickPending = false;
        NetworkDetonationPending = false;
    }

    private void ResetNetworkIfEmpty()
    {
        bool anyNetwork = false;
        foreach (GroundRune rune in _runes)
            anyNetwork |= rune.State == GroundRuneState.Network;
        if (!anyNetwork)
            _networkTimeRemaining = 0f;
    }

    private static bool TrySnapToGround(
        Vector2 desiredPosition,
        int roomId,
        DungeonMap dungeon,
        out Vector2 position)
    {
        position = default;
        if (dungeon == null)
            return false;

        DungeonRoom room = null;
        foreach (DungeonRoom candidate in dungeon.Rooms)
        {
            if (candidate.Id == roomId ||
                candidate.Bounds.Contains(desiredPosition.ToPoint()))
            {
                room = candidate;
                if (candidate.Id == roomId)
                    break;
            }
        }
        if (room == null)
            return false;

        float halfWidth = SpellbladeTuning.RuneWidth / 2f;
        float x = MathHelper.Clamp(
            desiredPosition.X,
            room.Bounds.Left + halfWidth + 4f,
            room.Bounds.Right - halfWidth - 4f);
        float groundY = room.GroundY;
        foreach (Platform platform in room.Platforms)
        {
            if (x >= platform.Bounds.Left + halfWidth &&
                x <= platform.Bounds.Right - halfWidth &&
                platform.Bounds.Top >= desiredPosition.Y - 24f &&
                platform.Bounds.Top < groundY)
            {
                groundY = platform.Bounds.Top;
            }
        }

        position = new Vector2(x, groundY - 2f);
        Rectangle probe = DungeonCollision.CreateBounds(
            position - new Vector2(0f, 7f),
            new Vector2(SpellbladeTuning.RuneWidth, 8f));
        return probe.Left >= room.Bounds.Left &&
            probe.Right <= room.Bounds.Right &&
            probe.Top >= dungeon.WorldBounds.Top &&
            probe.Bottom <= dungeon.WorldBounds.Bottom;
    }
}
