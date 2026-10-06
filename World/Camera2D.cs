using Microsoft.Xna.Framework;
using DungeonAscendant.Player;

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

    public void Follow(
        Vector2 targetPosition,
        FacingDirection facing,
        Rectangle worldBounds,
        float elapsedSeconds)
    {
        float playerScreenRatio = facing == FacingDirection.Right ? 0.43f : 0.57f;
        float desiredX = targetPosition.X - _viewportWidth * playerScreenRatio;
        float desiredY = targetPosition.Y - _viewportHeight / 2f;
        float maximumX = MathHelper.Max(
            worldBounds.Left,
            worldBounds.Right - _viewportWidth);
        float maximumY = MathHelper.Max(
            worldBounds.Top,
            worldBounds.Bottom - _viewportHeight);

        Vector2 clampedTarget = new(
            MathHelper.Clamp(desiredX, worldBounds.Left, maximumX),
            MathHelper.Clamp(desiredY, worldBounds.Top, maximumY));
        float horizontalBlend = 1f - System.MathF.Exp(-9f * elapsedSeconds);
        float verticalBlend = 1f - System.MathF.Exp(-4f * elapsedSeconds);
        Position = new Vector2(
            MathHelper.Lerp(Position.X, clampedTarget.X, horizontalBlend),
            MathHelper.Lerp(Position.Y, clampedTarget.Y, verticalBlend));
        Position = new Vector2(
            MathHelper.Clamp(Position.X, worldBounds.Left, maximumX),
            MathHelper.Clamp(Position.Y, worldBounds.Top, maximumY));
    }

    public void Snap(
        Vector2 targetPosition,
        FacingDirection facing,
        Rectangle worldBounds)
    {
        float playerScreenRatio = facing == FacingDirection.Right ? 0.43f : 0.57f;
        float maximumX = MathHelper.Max(worldBounds.Left, worldBounds.Right - _viewportWidth);
        float maximumY = MathHelper.Max(worldBounds.Top, worldBounds.Bottom - _viewportHeight);
        Position = new Vector2(
            MathHelper.Clamp(
                targetPosition.X - _viewportWidth * playerScreenRatio,
                worldBounds.Left,
                maximumX),
            MathHelper.Clamp(
                targetPosition.Y - _viewportHeight / 2f,
                worldBounds.Top,
                maximumY));
    }
}
