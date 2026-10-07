namespace DungeonAscendant.Combat;

public readonly struct CombatInput
{
    public bool LightPressed { get; }
    public bool HeavyPressed { get; }
    public bool HeavyHeld { get; }
    public bool HeavyReleased { get; }
    public bool DodgePressed { get; }
    public bool BlockHeld { get; }
    public float HorizontalDirection { get; }

    public CombatInput(
        bool lightPressed,
        bool heavyPressed,
        bool heavyHeld,
        bool heavyReleased,
        bool dodgePressed,
        bool blockHeld,
        float horizontalDirection)
    {
        LightPressed = lightPressed;
        HeavyPressed = heavyPressed;
        HeavyHeld = heavyHeld;
        HeavyReleased = heavyReleased;
        DodgePressed = dodgePressed;
        BlockHeld = blockHeld;
        HorizontalDirection = horizontalDirection;
    }
}
