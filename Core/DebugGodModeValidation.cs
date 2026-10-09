using System;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class DebugGodModeValidation
{
    public static void ValidateOrThrow()
    {
        var protectedPlayer = new Player.Player(Vector2.Zero);
        Require(protectedPlayer.DebugGodMode,
            "Debug God Mode must start enabled during Map testing.");
        Require(protectedPlayer.ReceiveDamage(int.MaxValue),
            "God Mode bypassed the normal damage detection pipeline.");
        Require(protectedPlayer.IsAlive && protectedPlayer.CurrentHealth == 1,
            "God Mode did not clamp lethal damage to one HP.");

        var mortalPlayer = new Player.Player(Vector2.Zero)
        {
            DebugGodMode = false
        };
        mortalPlayer.ReceiveDamage(int.MaxValue);
        Require(!mortalPlayer.IsAlive && mortalPlayer.CurrentHealth == 0,
            "Normal lethal damage changed while God Mode was disabled.");

        var session = new GameSession(new Rectangle(0, 0, 1280, 720), 1701);
        session.Update(Frame(), new KeyboardState(Keys.Enter), new MouseState());
        session.Update(Frame(), new KeyboardState(), new MouseState());
        Require(session.State == GameState.Playing && session.DebugGodMode,
            "Test session did not start in protected gameplay.");
        session.Update(Frame(), new KeyboardState(Keys.F9), new MouseState());
        Require(!session.DebugGodMode,
            "F9 did not disable Debug God Mode.");
        session.Player.ReceiveDamage(int.MaxValue);
        session.Update(Frame(), new KeyboardState(), new MouseState());
        Require(session.State == GameState.GameOver,
            "Game Over did not occur after disabling Debug God Mode.");
    }

    private static GameTime Frame() => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1d / 60d));

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
