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
    public float ChainMomentum { get; private set; }
    public float ChainMomentumRatio =>
        ReckonerTuning.MomentumRatio(ChainMomentum);
    public float BreakerInertia { get; private set; }
    public float BreakerInertiaRatio =>
        BreakerTuning.InertiaRatio(BreakerInertia);
    public int ActiveValue => Family switch
    {
        WeaponFamily.DualSwords => DuelistMomentum <= 0f
            ? 0
            : DuelistMomentumTier + 1,
        WeaponFamily.HunterBow => HunterFocus <= 0f
            ? 0
            : Math.Clamp((int)MathF.Ceiling(HunterFocus / 25f), 1, 4),
        WeaponFamily.ChainFlail => ChainMomentum <= 0f
            ? 0
            : Math.Clamp((int)MathF.Ceiling(ChainMomentum / 10f), 1, 10),
        WeaponFamily.SpikedMace => BreakerInertia <= 0f
            ? 0
            : Math.Clamp((int)MathF.Ceiling(BreakerInertia / 10f), 1, 10),
        _ => 0
    };
    public int ActiveMaximum => Family == WeaponFamily.HunterBow
        ? 4
        : Family is WeaponFamily.DualSwords or
        WeaponFamily.ChainFlail or
        WeaponFamily.SpikedMace ? 10 : 0;

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

        if (Family == WeaponFamily.SpikedMace)
        {
            if (_inactiveSeconds >= BreakerTuning.InertiaDecayDelaySeconds)
            {
                BreakerInertia = MathF.Max(
                    0f,
                    BreakerInertia - BreakerTuning.InertiaDecayPerSecond *
                        elapsedSeconds);
            }
            return;
        }

        if (Family == WeaponFamily.ChainFlail)
        {
            if (_inactiveSeconds >= ReckonerTuning.MomentumDecayDelaySeconds)
            {
                ChainMomentum = MathF.Max(
                    0f,
                    ChainMomentum - ReckonerTuning.MomentumDecayPerSecond *
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
        else if (Family == WeaponFamily.ChainFlail)
            AddChainMomentum(gain);
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
        MarkRangerCombatActivity();
        return amount;
    }

    public void MarkCombatActivity()
    {
        _inactiveSeconds = 0f;
        _decayClock = 0f;
    }

    public void AddChainMomentum(float amount)
    {
        if (Family != WeaponFamily.ChainFlail || amount <= 0f)
            return;

        ChainMomentum = MathF.Min(
            ReckonerTuning.MomentumMaximum,
            ChainMomentum + amount);
        MarkCombatActivity();
    }

    public void LoseChainMomentum(float amount)
    {
        if (Family != WeaponFamily.ChainFlail || amount <= 0f)
            return;

        ChainMomentum = MathF.Max(0f, ChainMomentum - amount);
        MarkCombatActivity();
    }

    public float ConsumeChainMomentum(float amount)
    {
        if (Family != WeaponFamily.ChainFlail || amount <= 0f)
            return 0f;

        float consumed = MathF.Min(ChainMomentum, amount);
        ChainMomentum -= consumed;
        MarkCombatActivity();
        return consumed;
    }

    public float SpendAllChainMomentum()
    {
        if (Family != WeaponFamily.ChainFlail)
            return 0f;

        float amount = ChainMomentum;
        ChainMomentum = 0f;
        MarkCombatActivity();
        return amount;
    }

    public void AddBreakerInertia(float amount)
    {
        if (Family != WeaponFamily.SpikedMace || amount <= 0f)
            return;

        BreakerInertia = MathF.Min(
            BreakerTuning.MaximumInertia,
            BreakerInertia + amount);
        MarkCombatActivity();
    }

    public void LoseBreakerInertia(float amount)
    {
        if (Family != WeaponFamily.SpikedMace || amount <= 0f)
            return;

        BreakerInertia = MathF.Max(0f, BreakerInertia - amount);
        MarkCombatActivity();
    }

    public float ConsumeBreakerInertia(float amount)
    {
        if (Family != WeaponFamily.SpikedMace || amount <= 0f)
            return 0f;

        float consumed = MathF.Min(BreakerInertia, amount);
        BreakerInertia -= consumed;
        MarkCombatActivity();
        return consumed;
    }

    public float ConsumeBreakerInertiaRatio(float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        return ConsumeBreakerInertia(BreakerInertia * ratio);
    }

    public float SpendAllBreakerInertia()
    {
        if (Family != WeaponFamily.SpikedMace)
            return 0f;

        float amount = BreakerInertia;
        BreakerInertia = 0f;
        MarkCombatActivity();
        return amount;
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
        else if (Family == WeaponFamily.SpikedMace)
        {
            LoseBreakerInertia(BreakerTuning.InterruptedAttackLoss);
        }
        else if (Family == WeaponFamily.ChainFlail)
        {
            LoseChainMomentum(heavyDamage
                ? ReckonerTuning.HeavyDamageMomentumLoss
                : ReckonerTuning.InterruptedMomentumLoss);
        }
    }

    public void Reset()
    {
        DuelistMomentum = 0;
        HunterFocus = 0f;
        ChainMomentum = 0f;
        BreakerInertia = 0f;
        _inactiveSeconds = 0f;
        _decayClock = 0f;
        _focusInactiveSeconds = 0f;
    }
}
