namespace DungeonAscendant.Combat;

public enum ThunderSpiritState
{
    Dormant,
    Active,
    Empowered,
    Discharged
}

public static class AncientStaffTuning
{
    public const float ChordWindowSeconds = .12f;
    public const int MaximumElderSigils = 3;
    public const float AegisDeflectWindowSeconds = .16f;
    public const float AegisDamageTakenMultiplier = .58f;
    public const float AegisPressureMultiplier = .82f;
    public const float ThunderSpiritDurationSeconds = 12f;
    public const float ThunderSpiritAttackIntervalSeconds = 1.15f;
    public const float ThunderSpiritRange = 430f;
    public const float RunePrisonRange = 460f;
    public const float RunePrisonRadius = 92f;
    public const float SpiritBastionRadius = 118f;
    public const float ElderInvocationRadius = 104f;
    public const float BlinkDistance = 124f;
    public const float FourthVeilDurationSeconds = 12f;
    public const float UltimateRange = 720f;
    public const float UltimateRadius = 235f;
    public const float UltimateIntroSeconds = .82f;
    public const float UltimateWorldSlowScale = .24f;
    public const float UltimateCameraZoom = 1.38f;
}
