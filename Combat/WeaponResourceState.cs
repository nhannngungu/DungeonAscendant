using System;
using DungeonAscendant.Items;

namespace DungeonAscendant.Combat;

public sealed class WeaponResourceState
{
    private const float OtherResourceDecayDelay = 2.25f;
    private const float OtherResourceDecayInterval = 1.15f;

    private float _inactiveSeconds;
    private float _decayClock;
    private float _focusInactiveSeconds;

    public WeaponFamily Family { get; private set; } = WeaponFamily.LongSword;
    public float DuelistMomentum { get; private set; }
    public float DuelistMomentumRatio =>
        DuelistTuning.GetRatio(DuelistMomentum);
    public int DuelistMomentumTier =>
        DuelistTuning.GetTier(DuelistMomentum);
    public float HunterFocus { get; private set; }
    public float HunterFocusRatio => RangerTuning.FocusRatio(HunterFocus);
    public float HunterUltimate { get; private set; }
    public float HunterUltimateRatio => Math.Clamp(
        HunterUltimate / RangerTuning.UltimateMaximum, 0f, 1f);
    public bool IsHunterUltimateReady =>
        HunterUltimate >= RangerTuning.UltimateMaximum;
    public int ElderSigils { get; private set; }
    public int ActiveValue => Family switch
    {
        WeaponFamily.DualSwords => DuelistMomentum <= 0f
            ? 0
            : DuelistMomentumTier + 1,
        WeaponFamily.HunterBow => HunterFocus <= 0f
            ? 0
            : Math.Clamp((int)MathF.Ceiling(HunterFocus / 25f), 1, 4),
        WeaponFamily.ArcaneWarStaff => ElderSigils,
        _ => 0
    };
    public int ActiveMaximum => Family == WeaponFamily.HunterBow
        ? 4
        : Family == WeaponFamily.ArcaneWarStaff
            ? AncientStaffTuning.MaximumElderSigils
        : Family == WeaponFamily.DualSwords ? 10 : 0;

    public void SetFamily(WeaponFamily family)
    {
        family = family.ToOfficialFamily();
        if (Family == family)
            return;

        Family = family;
        Reset();
    }

    public void Update(float elapsedSeconds)
    {
        elapsedSeconds = MathF.Max(0f, elapsedSeconds);
        _inactiveSeconds += elapsedSeconds;

        if (Family == WeaponFamily.DualSwords)
        {
            if (_inactiveSeconds >= DuelistTuning.DecayDelaySeconds)
            {
                DuelistMomentum = MathF.Max(
                    0f,
                    DuelistMomentum - DuelistTuning.DecayPerSecond *
                        elapsedSeconds);
            }
            return;
        }

        if (Family == WeaponFamily.HunterBow)
        {
            _focusInactiveSeconds += elapsedSeconds;
            if (_focusInactiveSeconds >= RangerTuning.FocusDecayDelaySeconds)
            {
                HunterFocus = MathF.Max(
                    0f,
                    HunterFocus - RangerTuning.FocusDecayPerSecond *
                        elapsedSeconds);
            }
            return;
        }

        if (_inactiveSeconds < OtherResourceDecayDelay)
            return;

        _decayClock += elapsedSeconds;
        if (_decayClock < OtherResourceDecayInterval)
            return;

        _decayClock = 0f;
    }

    public void RegisterHit(int gain)
    {
        _inactiveSeconds = 0f;
        _decayClock = 0f;
        gain = Math.Max(0, gain);

        if (Family == WeaponFamily.DualSwords)
            AddDuelistMomentum(gain);
        else if (Family == WeaponFamily.ArcaneWarStaff && gain > 0)
            AddElderSigil(gain);
    }

    public void AddElderSigil(int amount = 1)
    {
        if (Family != WeaponFamily.ArcaneWarStaff || amount <= 0)
            return;
        ElderSigils = Math.Min(
            AncientStaffTuning.MaximumElderSigils,
            ElderSigils + amount);
        MarkCombatActivity();
    }

    public void AddDuelistMomentum(float amount)
    {
        if (Family != WeaponFamily.DualSwords || amount <= 0f)
            return;

        DuelistMomentum = MathF.Min(
            DuelistTuning.MomentumMaximum,
            DuelistMomentum + amount);
        MarkCombatActivity();
    }

    public void LoseDuelistMomentum(float amount)
    {
        if (Family != WeaponFamily.DualSwords || amount <= 0f)
            return;
        DuelistMomentum = MathF.Max(0f, DuelistMomentum - amount);
    }

    public void AddHunterFocus(float amount)
    {
        if (Family != WeaponFamily.HunterBow || amount <= 0f)
            return;

        HunterFocus = MathF.Min(
            RangerTuning.FocusMaximum,
            HunterFocus + amount);
        MarkRangerCombatActivity();
    }

    public void LoseHunterFocus(float amount)
    {
        if (Family != WeaponFamily.HunterBow || amount <= 0f)
            return;

        HunterFocus = MathF.Max(0f, HunterFocus - amount);
    }

    public void AddHunterUltimate(float amount)
    {
        if (Family != WeaponFamily.HunterBow || amount <= 0f)
            return;

        HunterUltimate = MathF.Min(
            RangerTuning.UltimateMaximum,
            HunterUltimate + amount);
    }

    public bool ConsumeHunterUltimate()
    {
        if (Family != WeaponFamily.HunterBow || !IsHunterUltimateReady)
            return false;

        HunterUltimate = 0f;
        return true;
    }

    public void SetHunterFocusForDebug(float amount)
    {
        if (Family == WeaponFamily.HunterBow)
            HunterFocus = Math.Clamp(amount, 0f, RangerTuning.FocusMaximum);
    }

    public void SetHunterUltimateForDebug(float amount)
    {
        if (Family == WeaponFamily.HunterBow)
            HunterUltimate = Math.Clamp(amount, 0f, RangerTuning.UltimateMaximum);
    }

    public void MarkRangerCombatActivity()
    {
        _focusInactiveSeconds = 0f;
    }

    public float SpendAllHunterFocus()
    {
        if (Family != WeaponFamily.HunterBow)
            return 0f;

        float amount = HunterFocus;
        HunterFocus = 0f;
        HunterUltimate = 0f;
        MarkRangerCombatActivity();
        return amount;
    }

    public void MarkCombatActivity()
    {
        _inactiveSeconds = 0f;
        _decayClock = 0f;
    }

    public float SpendAllDuelistMomentum()
    {
        if (Family != WeaponFamily.DualSwords)
            return 0f;

        float amount = DuelistMomentum;
        DuelistMomentum = 0f;
        MarkCombatActivity();
        return amount;
    }

    public int Spend(int requested)
    {
        requested = Math.Max(0, requested);
        return 0;
    }

    public int SpendAll()
    {
        return 0;
    }

    public void OnOwnerDamaged(bool heavyDamage = true)
    {
        if (Family == WeaponFamily.DualSwords)
        {
            DuelistMomentum = MathF.Max(
                0f,
                DuelistMomentum - DuelistTuning.DamageMomentumLoss);
            MarkCombatActivity();
        }
        else if (Family == WeaponFamily.HunterBow)
        {
            LoseHunterFocus(RangerTuning.DamageFocusLoss);
            MarkRangerCombatActivity();
        }
    }

    public void Reset()
    {
        DuelistMomentum = 0;
        HunterFocus = 0f;
        HunterUltimate = 0f;
        _inactiveSeconds = 0f;
        _decayClock = 0f;
        _focusInactiveSeconds = 0f;
    }
}
