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
    public int ConstellationIndex { get; }
    public bool IsPortalStep { get; }
    public SovereignPortalPhase Phase { get; private set; }
    public float PhaseRatio { get; private set; }
    public float OpenRatio { get; private set; }
    public float ActiveRatio { get; private set; }
    internal float Age;
    private readonly float _presentationSeconds;

    internal SovereignPortal(
        SovereignPortalType type,
        Vector2 position,
        float rotation,
        float scale,
        bool logical,
        SovereignPortalFlow flow,
        Vector2 direction,
        int pairId,
        int constellationIndex,
        bool portalStep,
        float presentationSeconds)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
        Scale = Math.Clamp(scale, .35f, 2.2f);
        IsLogical = logical;
        Flow = flow;
        PairId = pairId;
        ConstellationIndex = constellationIndex;
        IsPortalStep = portalStep;
        _presentationSeconds = MathF.Max(0f, presentationSeconds);
        Direction = direction.LengthSquared() > .001f
            ? Vector2.Normalize(direction)
            : new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));
    }

    internal bool Update(float elapsedSeconds)
    {
        Age += MathF.Max(0f, elapsedSeconds);
        float anticipationSeconds = IsPortalStep
            ? _presentationSeconds * .18f
            : AbyssRingTuning.PortalAnticipationSeconds;
        float openSeconds = IsPortalStep
            ? _presentationSeconds * .20f
            : AbyssRingTuning.PortalOpenSeconds;
        float stabilizeSeconds = IsPortalStep
            ? _presentationSeconds * .15f
            : AbyssRingTuning.PortalStabilizeSeconds;
        float activeSeconds = IsPortalStep
            ? _presentationSeconds * .15f
            : Type == SovereignPortalType.Decorative
                ? AbyssRingTuning.UltimateConstellationHoldSeconds
                : AbyssRingTuning.PortalActiveSeconds;
        float closeSeconds = IsPortalStep
            ? _presentationSeconds * .32f
            : AbyssRingTuning.PortalCloseSeconds;
        anticipationSeconds = MathF.Max(.006f, anticipationSeconds);
        openSeconds = MathF.Max(.006f, openSeconds);
        stabilizeSeconds = MathF.Max(.006f, stabilizeSeconds);
        activeSeconds = MathF.Max(.008f, activeSeconds);
        closeSeconds = MathF.Max(.008f, closeSeconds);
        float anticipationEnd = anticipationSeconds;
        float openEnd = anticipationEnd + openSeconds;
        float stabilizeEnd = openEnd + stabilizeSeconds;
        float activeEnd = stabilizeEnd + activeSeconds;
        float lifetime = activeEnd + closeSeconds;
        ActiveRatio = 0f;
        if (Age < anticipationEnd)
        {
            Phase = SovereignPortalPhase.Anticipating;
            PhaseRatio = Age / anticipationSeconds;
            OpenRatio = .08f + PhaseRatio * .12f;
        }
        else if (Age < openEnd)
        {
            Phase = SovereignPortalPhase.Opening;
            PhaseRatio = (Age - anticipationEnd) / openSeconds;
            OpenRatio = MathHelper.SmoothStep(.20f, 1f, PhaseRatio);
        }
        else if (Age < stabilizeEnd)
        {
            Phase = SovereignPortalPhase.Stabilizing;
            PhaseRatio = (Age - openEnd) / stabilizeSeconds;
            OpenRatio = 1f + MathF.Sin(PhaseRatio * MathHelper.Pi) * .07f;
        }
        else if (Age < activeEnd)
        {
            Phase = SovereignPortalPhase.Active;
            PhaseRatio = 1f;
            OpenRatio = 1f;
            ActiveRatio = (Age - stabilizeEnd) / activeSeconds;
        }
        else
        {
            Phase = SovereignPortalPhase.Closing;
            PhaseRatio = 1f - (Age - activeEnd) / closeSeconds;
            OpenRatio = MathHelper.SmoothStep(0f, 1f, PhaseRatio);
            ActiveRatio = 1f;
        }
        return Age < lifetime;
    }
}

public sealed class SovereignPortalSystem
{
    private static readonly Vector2[] UltimateConstellationOffsets =
    {
        new(-104f, 31f),
        new(-54f, -79f),
        new(103f, -31f),
        new(73f, 57f),
        new(-116f, -18f),
        new(18f, -108f)
    };
    private static readonly float[] UltimateConstellationScales =
    {
        .84f, .96f, .90f, .78f, .88f, 1.22f
    };
    private static readonly int[] UltimateStepRoute =
    {
        0, 2, 1, 3, 4, 2, 0, 1,
        4, 3, 2, 1, 0, 4, 3, 5
    };

    private readonly List<SovereignPortal> _portals = new(48);
    private int _nextPairId;
    public IReadOnlyList<SovereignPortal> Portals => _portals;
    public bool HasUltimateConstellation
    {
        get
        {
            for (int index = 0; index < _portals.Count; index++)
                if (_portals[index].ConstellationIndex >= 0)
                    return true;
            return false;
        }
    }

    public static int GetUltimateStepPortalIndex(int stage)
    {
        if (stage <= 0)
            return 0;
        if (stage > AbyssRingTuning.UltimateStrikeCount)
            return AbyssRingTuning.UltimateConstellationCount - 1;
        return UltimateStepRoute[(stage - 1) % UltimateStepRoute.Length];
    }

    public static Vector2 GetUltimateConstellationPosition(
        Vector2 center,
        int index) =>
        center + UltimateConstellationOffsets[Math.Clamp(
            index,
            0,
            UltimateConstellationOffsets.Length - 1)];

    public void Open(
        SovereignPortalType type,
        Vector2 position,
        float rotation = 0f,
        float scale = 1f,
        bool logical = true,
        SovereignPortalFlow flow = SovereignPortalFlow.Neutral,
        Vector2? direction = null,
        int pairId = 0,
        int constellationIndex = -1,
        bool portalStep = false,
        float presentationSeconds = 0f)
    {
        if (_portals.Count >= 48)
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
            pairId,
            constellationIndex,
            portalStep,
            presentationSeconds));
    }

    public void OpenPair(
        SovereignPortalType type,
        Vector2 entry,
        Vector2 exit,
        float scale = 1f,
        bool portalStep = false,
        float presentationSeconds = 0f)
    {
        Vector2 direction = exit - entry;
        if (direction.LengthSquared() < .001f)
            direction = Vector2.UnitX;
        direction.Normalize();
        float rotation = MathF.Atan2(direction.Y, direction.X);
        int pairId = ++_nextPairId;
        Open(type, entry, rotation, scale, logical: true,
            SovereignPortalFlow.Entry, direction, pairId,
            portalStep: portalStep,
            presentationSeconds: presentationSeconds);
        Open(type, exit, rotation, scale, logical: true,
            SovereignPortalFlow.Exit, direction, pairId,
            portalStep: portalStep,
            presentationSeconds: presentationSeconds);
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
        if (exit.IsPortalStep)
        {
            switch (exit.Phase)
            {
                case SovereignPortalPhase.Anticipating:
                    position = entry.Position;
                    transitionStrength = .05f + exit.PhaseRatio * .20f;
                    break;
                case SovereignPortalPhase.Opening:
                    float launch = MathHelper.SmoothStep(
                        0f,
                        1f,
                        exit.PhaseRatio);
                    Vector2 normal = new(-direction.Y, direction.X);
                    position = Vector2.Lerp(
                        entry.Position,
                        exit.Position,
                        launch) - normal * MathF.Sin(launch * MathHelper.Pi) * 7f;
                    transitionStrength = .25f + launch * .45f;
                    break;
                case SovereignPortalPhase.Stabilizing:
                    position = exit.Position + direction *
                        (4f * MathF.Sin(exit.PhaseRatio * MathHelper.Pi));
                    transitionStrength = .70f + exit.PhaseRatio * .18f;
                    break;
                case SovereignPortalPhase.Active:
                    position = exit.Position;
                    transitionStrength = .88f +
                        Math.Min(1f, exit.ActiveRatio * 2f) * .12f;
                    break;
                default:
                    position = exit.Position;
                    break;
            }
            return true;
        }

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
        for (int index = _portals.Count - 1; index >= 0; index--)
            if (_portals[index].ConstellationIndex >= 0)
                _portals.RemoveAt(index);
        for (int index = 0; index < AbyssRingTuning.GoldenGateCount; index++)
        {
            Vector2 position = GetUltimateConstellationPosition(center, index);
            int nextIndex = index == AbyssRingTuning.GoldenGateCount - 1
                ? 2
                : index + 1;
            Vector2 direction =
                GetUltimateConstellationPosition(center, nextIndex) - position;
            if (direction.LengthSquared() < .001f)
                direction = Vector2.UnitX;
            direction.Normalize();
            Open(SovereignPortalType.Decorative,
                position,
                MathF.Atan2(direction.Y, direction.X),
                UltimateConstellationScales[index],
                logical: false,
                direction: direction,
                constellationIndex: index);
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
