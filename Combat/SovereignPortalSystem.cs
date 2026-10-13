using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public enum SovereignPortalType
{
    Transit,
    Strike,
    Decorative
}

public enum SovereignPortalPhase
{
    Anticipating,
    Opening,
    Stabilizing,
    Active,
    Closing
}

public enum SovereignPortalFlow
{
    Neutral,
    Entry,
    Exit
}

public sealed class SovereignPortal
{
    public SovereignPortalType Type { get; }
    public Vector2 Position { get; }
    public float Rotation { get; }
    public float Scale { get; }
    public bool IsLogical { get; }
    public SovereignPortalFlow Flow { get; }
    public Vector2 Direction { get; }
    public int PairId { get; }
    public SovereignPortalPhase Phase { get; private set; }
    public float PhaseRatio { get; private set; }
    public float OpenRatio { get; private set; }
    public float ActiveRatio { get; private set; }
    internal float Age;

    internal SovereignPortal(
        SovereignPortalType type,
        Vector2 position,
        float rotation,
        float scale,
        bool logical,
        SovereignPortalFlow flow,
        Vector2 direction,
        int pairId)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
        Scale = Math.Clamp(scale, .35f, 2.2f);
        IsLogical = logical;
        Flow = flow;
        PairId = pairId;
        Direction = direction.LengthSquared() > .001f
            ? Vector2.Normalize(direction)
            : new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));
    }

    internal bool Update(float elapsedSeconds)
    {
        Age += MathF.Max(0f, elapsedSeconds);
        float anticipationEnd = AbyssRingTuning.PortalAnticipationSeconds;
        float openEnd = anticipationEnd + AbyssRingTuning.PortalOpenSeconds;
        float stabilizeEnd = openEnd + AbyssRingTuning.PortalStabilizeSeconds;
        float activeEnd = stabilizeEnd + AbyssRingTuning.PortalActiveSeconds;
        float lifetime = activeEnd + AbyssRingTuning.PortalCloseSeconds;
        ActiveRatio = 0f;
        if (Age < anticipationEnd)
        {
            Phase = SovereignPortalPhase.Anticipating;
            PhaseRatio = Age / AbyssRingTuning.PortalAnticipationSeconds;
            OpenRatio = .08f + PhaseRatio * .12f;
        }
        else if (Age < openEnd)
        {
            Phase = SovereignPortalPhase.Opening;
            PhaseRatio = (Age - anticipationEnd) /
                AbyssRingTuning.PortalOpenSeconds;
            OpenRatio = MathHelper.SmoothStep(.20f, 1f, PhaseRatio);
        }
        else if (Age < stabilizeEnd)
        {
            Phase = SovereignPortalPhase.Stabilizing;
            PhaseRatio = (Age - openEnd) /
                AbyssRingTuning.PortalStabilizeSeconds;
            OpenRatio = 1f + MathF.Sin(PhaseRatio * MathHelper.Pi) * .07f;
        }
        else if (Age < activeEnd)
        {
            Phase = SovereignPortalPhase.Active;
            PhaseRatio = 1f;
            OpenRatio = 1f;
            ActiveRatio = (Age - stabilizeEnd) /
                AbyssRingTuning.PortalActiveSeconds;
        }
        else
        {
            Phase = SovereignPortalPhase.Closing;
            PhaseRatio = 1f - (Age - activeEnd) /
                AbyssRingTuning.PortalCloseSeconds;
            OpenRatio = MathHelper.SmoothStep(0f, 1f, PhaseRatio);
            ActiveRatio = 1f;
        }
        return Age < lifetime;
    }
}

public sealed class SovereignPortalSystem
{
    private readonly List<SovereignPortal> _portals = new(32);
    private int _nextPairId;
    public IReadOnlyList<SovereignPortal> Portals => _portals;

    public void Open(
        SovereignPortalType type,
        Vector2 position,
        float rotation = 0f,
        float scale = 1f,
        bool logical = true,
        SovereignPortalFlow flow = SovereignPortalFlow.Neutral,
        Vector2? direction = null,
        int pairId = 0)
    {
        if (_portals.Count >= 40)
            _portals.RemoveAt(0);
        _portals.Add(new SovereignPortal(
            type,
            position,
            rotation,
            scale,
            logical,
            flow,
            direction ?? new Vector2(
                MathF.Cos(rotation),
                MathF.Sin(rotation)),
            pairId));
    }

    public void OpenPair(
        SovereignPortalType type,
        Vector2 entry,
        Vector2 exit,
        float scale = 1f)
    {
        Vector2 direction = exit - entry;
        if (direction.LengthSquared() < .001f)
            direction = Vector2.UnitX;
        direction.Normalize();
        float rotation = MathF.Atan2(direction.Y, direction.X);
        int pairId = ++_nextPairId;
        Open(type, entry, rotation, scale, logical: true,
            SovereignPortalFlow.Entry, direction, pairId);
        Open(type, exit, rotation, scale, logical: true,
            SovereignPortalFlow.Exit, direction, pairId);
    }

    public bool TryGetTransitVisual(
        out Vector2 position,
        out Vector2 direction,
        out float transitionStrength)
    {
        position = Vector2.Zero;
        direction = Vector2.UnitX;
        transitionStrength = 0f;
        SovereignPortal exit = null;
        SovereignPortal entry = null;
        for (int index = _portals.Count - 1; index >= 0; index--)
        {
            SovereignPortal portal = _portals[index];
            if (portal.Type != SovereignPortalType.Transit ||
                portal.PairId == 0)
                continue;
            if (exit == null && portal.Flow == SovereignPortalFlow.Exit)
                exit = portal;
            else if (exit != null &&
                portal.PairId == exit.PairId &&
                portal.Flow == SovereignPortalFlow.Entry)
            {
                entry = portal;
                break;
            }
        }
        if (entry == null || exit == null)
            return false;

        direction = exit.Direction;
        switch (exit.Phase)
        {
            case SovereignPortalPhase.Anticipating:
                position = entry.Position;
                transitionStrength = exit.PhaseRatio * .25f;
                break;
            case SovereignPortalPhase.Opening:
                position = entry.Position + direction *
                    (8f * exit.PhaseRatio);
                transitionStrength = .25f + exit.PhaseRatio * .55f;
                break;
            case SovereignPortalPhase.Stabilizing:
                if (exit.PhaseRatio < .5f)
                {
                    float entryRatio = exit.PhaseRatio * 2f;
                    position = entry.Position + direction *
                        (8f + entryRatio * 15f);
                    transitionStrength = .8f + entryRatio * .2f;
                }
                else
                {
                    float exitRatio = (exit.PhaseRatio - .5f) * 2f;
                    position = exit.Position - direction *
                        (18f * (1f - exitRatio));
                    transitionStrength = 1f - exitRatio * .35f;
                }
                break;
            case SovereignPortalPhase.Active:
                float settle = Math.Min(1f, exit.ActiveRatio * 3.5f);
                position = exit.Position - direction * (6f * (1f - settle));
                transitionStrength = (1f - settle) * .65f;
                break;
            default:
                position = exit.Position;
                break;
        }
        return true;
    }

    public void OpenGoldenGateArray(Vector2 center)
    {
        Clear();
        for (int index = 0; index < AbyssRingTuning.GoldenGateCount; index++)
        {
            float angle = MathHelper.TwoPi * index /
                AbyssRingTuning.GoldenGateCount +
                MathF.Sin(index * 1.71f) * .14f;
            float radius = 78f + (index * 7 % 5) * 17f;
            float depth = .43f + index % 4 * .055f;
            Vector2 offset = new(
                MathF.Cos(angle) * radius + MathF.Sin(angle * 2f) * 11f,
                MathF.Sin(angle) * radius * depth);
            float scale = .52f + index % 5 * .105f +
                (MathF.Sin(angle) + 1f) * .055f;
            Open(SovereignPortalType.Decorative,
                center + offset,
                angle + MathHelper.PiOver2,
                scale,
                logical: false);
        }
    }

    public void OpenTheatreArray(Vector2 center)
    {
        for (int index = 0; index < 6; index++)
        {
            float angle = MathHelper.TwoPi * index / 6f;
            Vector2 offset = new(
                MathF.Cos(angle) * 78f,
                MathF.Sin(angle) * 48f);
            Open(
                SovereignPortalType.Transit,
                center + offset,
                angle + MathHelper.PiOver2,
                .82f,
                logical: true);
        }
    }

    public void Update(float elapsedSeconds)
    {
        for (int index = _portals.Count - 1; index >= 0; index--)
            if (!_portals[index].Update(elapsedSeconds))
                _portals.RemoveAt(index);
    }

    public void Clear() => _portals.Clear();
}
