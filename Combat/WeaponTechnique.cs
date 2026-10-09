using System;

namespace DungeonAscendant.Combat;

/// <summary>
/// Immutable, data-driven description of one weapon technique.  Multi-stage
/// moves are represented by ordered attacks and pay stamina only once.
/// </summary>
public sealed class WeaponTechnique
{
    private readonly AttackDefinition[] _stages;

    public string Id { get; }
    public string Name { get; }
    public WeaponTechniqueInput Input { get; }
    public float StaminaCost { get; }
    public bool RequiresFullStamina { get; }
    public bool ProvidesFrontalGuard { get; }
    public WeaponTechniqueEffect Effect { get; }
    public int ResourceGainOnHit { get; }
    public int ResourceCost { get; }
    public float HitstopSeconds { get; }
    public bool RequiresRepeatedInput { get; }
    public float AdditionalStageStaminaCost { get; }
    public int StageCount => _stages.Length;

    public WeaponTechnique(
        string id,
        string name,
        WeaponTechniqueInput input,
        float staminaCost,
        AttackDefinition[] stages,
        bool requiresFullStamina = false,
        bool providesFrontalGuard = false,
        WeaponTechniqueEffect effect = WeaponTechniqueEffect.None,
        int resourceGainOnHit = 0,
        int resourceCost = 0,
        float hitstopSeconds = .035f,
        bool requiresRepeatedInput = false,
        float additionalStageStaminaCost = 0f)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Input = input;
        StaminaCost = MathF.Max(0f, staminaCost);
        RequiresFullStamina = requiresFullStamina;
        ProvidesFrontalGuard = providesFrontalGuard;
        Effect = effect;
        ResourceGainOnHit = Math.Max(0, resourceGainOnHit);
        ResourceCost = Math.Max(0, resourceCost);
        HitstopSeconds = MathF.Max(0f, hitstopSeconds);
        RequiresRepeatedInput = requiresRepeatedInput;
        AdditionalStageStaminaCost = MathF.Max(0f, additionalStageStaminaCost);
        _stages = stages == null ? Array.Empty<AttackDefinition>() :
            (AttackDefinition[])stages.Clone();
    }

    public AttackDefinition GetStage(int index)
    {
        return index >= 0 && index < _stages.Length ? _stages[index] : null;
    }
}
