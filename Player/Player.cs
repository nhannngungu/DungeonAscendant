using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Player;

/// <summary>
/// Owns the player character's state and movement behavior.
/// </summary>
public sealed class Player
{
    public Vector2 Position { get; private set; }
    public float MovementSpeed { get; }
    public Vector2 Size { get; }

    public Player(Vector2 position, float movementSpeed = 220f)
    {
        Position = position;
        MovementSpeed = movementSpeed;
        Size = new Vector2(40f, 56f);
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Vector2 movement = Vector2.Zero;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            movement.Y -= 1f;

        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            movement.Y += 1f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            movement.X -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            movement.X += 1f;

        if (movement != Vector2.Zero)
            movement.Normalize();

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += movement * MovementSpeed * elapsedSeconds;
    }
}
