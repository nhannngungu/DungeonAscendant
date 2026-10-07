using System;

namespace DungeonAscendant.Combat;

public sealed class WeaponMoveSet
{
    private readonly AttackDefinition[] _lightCombo;

    public string Id { get; }
    public int ComboLength => _lightCombo.Length;
    public AttackDefinition Heavy { get; }
    public float HeavyChargeSeconds { get; }
    public bool HasChargedHeavy => HeavyChargeSeconds > 0f;

    public WeaponMoveSet(
        string id,
        AttackDefinition[] lightCombo,
        AttackDefinition heavy,
        float heavyChargeSeconds = 0f)
    {
        Id = id ?? string.Empty;
        if (lightCombo == null || lightCombo.Length == 0)
            throw new ArgumentException("A moveset needs at least one light attack.", nameof(lightCombo));

        _lightCombo = (AttackDefinition[])lightCombo.Clone();
        for (int index = 0; index < _lightCombo.Length; index++)
        {
            if (_lightCombo[index] == null)
                throw new ArgumentNullException(nameof(lightCombo));
        }

        Heavy = heavy ?? throw new ArgumentNullException(nameof(heavy));
        HeavyChargeSeconds = MathF.Max(0f, heavyChargeSeconds);
    }

    public AttackDefinition GetLight(int index)
    {
        int safeIndex = Math.Clamp(index, 0, _lightCombo.Length - 1);
        return _lightCombo[safeIndex];
    }
}
