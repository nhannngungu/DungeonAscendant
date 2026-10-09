namespace DungeonAscendant.Combat;

public readonly struct CombatInput
{
    public bool Skill1Pressed { get; }
    public bool Skill2Pressed { get; }
    public bool Skill3Pressed { get; }
    public bool DodgePressed { get; }
    public bool DashHeld { get; }
    public bool BlockHeld { get; }
    public float HorizontalDirection { get; }
    public bool PerfectEvadeOpportunity { get; }
    public bool Skill1Held { get; }
    public bool Skill1Released { get; }
    public bool Skill3Held { get; }
    public bool Skill3Released { get; }
    public bool Skill2Held { get; }
    public bool Skill2Released { get; }

    public CombatInput(
        bool skill1Pressed,
        bool skill2Pressed,
        bool skill3Pressed,
        bool dodgePressed,
        bool dashHeld,
        bool blockHeld,
        float horizontalDirection,
        bool perfectEvadeOpportunity = false,
        bool skill1Held = false,
        bool skill1Released = false,
        bool skill3Held = false,
        bool skill3Released = false,
        bool skill2Held = false,
        bool skill2Released = false)
    {
        Skill1Pressed = skill1Pressed;
        Skill2Pressed = skill2Pressed;
        Skill3Pressed = skill3Pressed;
        DodgePressed = dodgePressed;
        DashHeld = dashHeld;
        BlockHeld = blockHeld;
        HorizontalDirection = horizontalDirection;
        PerfectEvadeOpportunity = perfectEvadeOpportunity;
        Skill1Held = skill1Held;
        Skill1Released = skill1Released;
        Skill3Held = skill3Held;
        Skill3Released = skill3Released;
        Skill2Held = skill2Held;
        Skill2Released = skill2Released;
    }
}
