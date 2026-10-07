using System;
using System.Collections.Generic;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Selects presentation animations from read-only player state. Finite combat
/// actions synchronize to combat elapsed time while locomotion and death keep
/// independent visual time.
/// </summary>
public sealed class SpriteAnimationController
{
    private static readonly IReadOnlyDictionary<PlayerVisualState, SpriteAnimation>
        Animations = new Dictionary<PlayerVisualState, SpriteAnimation>
        {
            [PlayerVisualState.Idle] = new(6, 0.12f, true),
            [PlayerVisualState.Run] = new(8, 0.08f, true),
            [PlayerVisualState.Jump] = new(2, 0.12f, false),
            [PlayerVisualState.Fall] = new(2, 0.14f, true),
            [PlayerVisualState.LightAttack1] = new(6, 0.06f, false),
            [PlayerVisualState.LightAttack2] = new(6, 0.065f, false),
            [PlayerVisualState.LightAttack3] = new(9, 0.0667f, false),
            [PlayerVisualState.LightAttack4] = new(6, 0.05f, false),
            [PlayerVisualState.LightAttack5] = new(9, 0.05f, false),
            [PlayerVisualState.HeavyCharge] = new(6, 0.10f, true),
            [PlayerVisualState.HeavyAttack] = new(6, 0.1467f, false),
            [PlayerVisualState.Dodge] = new(5, 0.068f, false),
            [PlayerVisualState.Block] = new(2, 0.08f, false),
            [PlayerVisualState.GuardBreak] = new(4, 0.175f, false),
            [PlayerVisualState.Hurt] = new(4, 0.055f, false),
            [PlayerVisualState.Dead] = new(4, 0.14f, false),
            [PlayerVisualState.PotionUse] = new(4, 0.12f, false)
        };

    private float _elapsed;

    public PlayerVisualState State { get; private set; } = PlayerVisualState.Idle;
    public int Frame => Animations[State].GetFrame(_elapsed);
    public float Progress => Animations[State].GetProgress(_elapsed);
    public float Elapsed => _elapsed;

    public void Update(GameTime gameTime, PlayerCharacter player)
    {
        PlayerVisualState nextState = player.VisualState;
        bool changed = nextState != State;

        if (changed)
        {
            State = nextState;
            _elapsed = 0f;
        }

        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 10f);

        if (UsesCombatClock(State))
            _elapsed = player.Combat.StateElapsed;
        else if (!changed || State == PlayerVisualState.Dead)
            _elapsed += elapsedSeconds;
    }

    private static bool UsesCombatClock(PlayerVisualState state)
    {
        return state == PlayerVisualState.LightAttack1 ||
            state == PlayerVisualState.LightAttack2 ||
            state == PlayerVisualState.LightAttack3 ||
            state == PlayerVisualState.LightAttack4 ||
            state == PlayerVisualState.LightAttack5 ||
            state == PlayerVisualState.HeavyCharge ||
            state == PlayerVisualState.HeavyAttack ||
            state == PlayerVisualState.Dodge ||
            state == PlayerVisualState.GuardBreak ||
            state == PlayerVisualState.Hurt;
    }
}
