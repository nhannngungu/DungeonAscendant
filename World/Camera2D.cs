using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public sealed class Camera2D
{
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;

    public Vector2 Position { get; private set; }
    public Matrix Transform => Matrix.CreateTranslation(
        -Position.X,
        -Position.Y,
        0f);
    public Rectangle ViewBounds => new(
        (int)Position.X,
        (int)Position.Y,
        _viewportWidth,
        _viewportHeight);

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
    }

    public void Follow(Vector2 targetPosition, Rectangle worldBounds)
    {
        float desiredX = targetPosition.X - _viewportWidth / 2f;
        float desiredY = targetPosition.Y - _viewportHeight / 2f;
        float maximumX = MathHelper.Max(
            worldBounds.Left,
            worldBounds.Right - _viewportWidth);
        float maximumY = MathHelper.Max(
            worldBounds.Top,
            worldBounds.Bottom - _viewportHeight);

        Position = new Vector2(
            MathHelper.Clamp(desiredX, worldBounds.Left, maximumX),
            MathHelper.Clamp(desiredY, worldBounds.Top, maximumY));
    }
}
