using System;

namespace DungeonAscendant.Combat;

/// <summary>
/// Collects J/K/L press edges into an order-independent chord.  Triple input
/// resolves immediately; dual and single inputs resolve when the 0.30 second
/// window closes so lower-priority actions cannot fire first.
/// </summary>
public sealed class CombatInputBuffer
{
    public const float DefaultWindowSeconds = .30f;

    private readonly float _windowSeconds;
    private float _remaining;
    private int _mask;

    public bool HasPendingInput => _mask != 0;

    public CombatInputBuffer(float windowSeconds = DefaultWindowSeconds)
    {
        _windowSeconds = MathF.Max(.05f, windowSeconds);
    }

    public WeaponTechniqueInput? Update(
        float elapsedSeconds,
        bool skill1Pressed,
        bool skill2Pressed,
        bool skill3Pressed)
    {
        int pressedMask = (skill1Pressed ? 1 : 0) |
            (skill2Pressed ? 2 : 0) |
            (skill3Pressed ? 4 : 0);

        if (pressedMask != 0)
        {
            if (_mask == 0)
                _remaining = _windowSeconds;

            _mask |= pressedMask;
        }

        if (_mask == 7)
            return Consume();

        if (_mask == 0)
            return null;

        _remaining -= MathF.Max(0f, elapsedSeconds);
        return _remaining <= 0f ? Consume() : null;
    }

    public void Clear()
    {
        _mask = 0;
        _remaining = 0f;
    }

    private WeaponTechniqueInput Consume()
    {
        WeaponTechniqueInput result = _mask switch
        {
            7 => WeaponTechniqueInput.Ultimate,
            3 => WeaponTechniqueInput.Skill1Skill2,
            5 => WeaponTechniqueInput.Skill1Skill3,
            6 => WeaponTechniqueInput.Skill2Skill3,
            1 => WeaponTechniqueInput.Skill1,
            2 => WeaponTechniqueInput.Skill2,
            _ => WeaponTechniqueInput.Skill3
        };
        Clear();
        return result;
    }
}
