using System;
using System.Collections.Generic;

namespace DungeonAscendant.Combat;

public sealed class WeaponCombatProfile
{
    private readonly Dictionary<WeaponTechniqueInput, WeaponTechnique> _techniques;

    public string Id { get; }
    public WeaponTechniqueInput DashInput { get; }

    public WeaponCombatProfile(
        string id,
        WeaponTechniqueInput dashInput,
        params WeaponTechnique[] techniques)
    {
        Id = id ?? string.Empty;
        DashInput = dashInput;
        _techniques = new Dictionary<WeaponTechniqueInput, WeaponTechnique>();

        foreach (WeaponTechnique technique in techniques ?? Array.Empty<WeaponTechnique>())
        {
            if (technique != null)
                _techniques[technique.Input] = technique;
        }
    }

    public WeaponTechnique Get(WeaponTechniqueInput input)
    {
        return _techniques.TryGetValue(input, out WeaponTechnique technique)
            ? technique
            : null;
    }
}
