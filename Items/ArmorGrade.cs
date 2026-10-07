using System;

namespace DungeonAscendant.Items;

public enum ArmorGrade
{
    C1 = 1,
    C2 = 2,
    C3 = 3,
    C4 = 4,
    C5 = 5
}

public static class ArmorGradeRules
{
    public const ArmorGrade Minimum = ArmorGrade.C1;
    public const ArmorGrade Maximum = ArmorGrade.C5;
    public const int FusionMaterialCount = 3;

    // Intentionally modest. These values are the single tuning point for
    // grade power and do not replace armor class identity or item rarity.
    private static readonly float[] PowerMultipliers =
    {
        1.00f,
        1.12f,
        1.25f,
        1.39f,
        1.54f
    };

    public static ArmorGrade Clamp(ArmorGrade grade)
    {
        return (ArmorGrade)Math.Clamp((int)grade, (int)Minimum, (int)Maximum);
    }

    public static bool CanAdvance(ArmorGrade grade) => Clamp(grade) < Maximum;

    public static ArmorGrade Next(ArmorGrade grade)
    {
        ArmorGrade safeGrade = Clamp(grade);
        return CanAdvance(safeGrade)
            ? (ArmorGrade)((int)safeGrade + 1)
            : Maximum;
    }

    public static string ToDisplayName(ArmorGrade grade) => Clamp(grade).ToString();

    public static float GetPowerMultiplier(ArmorGrade grade)
    {
        return PowerMultipliers[(int)Clamp(grade) - 1];
    }

    public static int ScaleBonus(int baseValue, ArmorGrade grade)
    {
        return Math.Max(0, (int)MathF.Round(baseValue * GetPowerMultiplier(grade)));
    }

    public static float ScaleDamageTaken(float baseMultiplier, ArmorGrade grade)
    {
        float baseReduction = MathF.Max(0f, 1f - baseMultiplier);
        float scaledReduction = baseReduction * GetPowerMultiplier(grade);
        return Math.Clamp(1f - scaledReduction, .45f, 2f);
    }

    public static float ScaleStrength(float baseModifier, ArmorGrade grade)
    {
        if (baseModifier <= 1f)
            return baseModifier;

        return 1f + (baseModifier - 1f) * GetPowerMultiplier(grade);
    }

    public static float ScaleCostReduction(float baseModifier, ArmorGrade grade)
    {
        if (baseModifier >= 1f)
            return baseModifier;

        return Math.Clamp(
            1f - (1f - baseModifier) * GetPowerMultiplier(grade),
            .1f,
            1f);
    }
}
