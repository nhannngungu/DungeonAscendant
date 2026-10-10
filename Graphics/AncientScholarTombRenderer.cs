using System;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only stonework, dust, and non-hostile scholar presentation.
/// </summary>
public sealed class AncientScholarTombRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public AncientScholarTombRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(
        AncientScholarTombSystem tomb,
        Rectangle cameraBounds,
        GameTime gameTime)
    {
        if (tomb?.IsAvailable != true)
            return;

        Rectangle cull = tomb.CoffinBounds;
        cull.Inflate(90, 105);
        if (!cull.Intersects(cameraBounds))
            return;

        Rectangle coffin = tomb.CoffinBounds;
        DrawSafeSigil(tomb.Position, gameTime);
        DrawCoffinBase(coffin);

        if (tomb.ScholarRevealProgress > 0f)
            DrawScholar(tomb, gameTime);

        DrawLid(coffin, tomb.OpeningProgress);

        if (tomb.State == AncientScholarTombState.Opening)
            DrawDust(tomb, gameTime);
    }

    private void DrawSafeSigil(Vector2 position, GameTime gameTime)
    {
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 1.7f);
        Color glow = new Color(104, 173, 181, (int)(22 + pulse * 15));
        int x = (int)position.X;
        int y = (int)position.Y - 3;
        _spriteBatch.Draw(_pixel, new Rectangle(x - 108, y, 216, 2), glow);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 72, y - 4, 144, 1), glow);
        for (int index = -3; index <= 3; index++)
        {
            int runeX = x + index * 29;
            _spriteBatch.Draw(_pixel, new Rectangle(runeX - 4, y - 6, 8, 2), glow);
            _spriteBatch.Draw(_pixel, new Rectangle(runeX - 1, y - 11, 2, 7), glow);
        }
    }

    private void DrawCoffinBase(Rectangle coffin)
    {
        Color shadow = new(18, 20, 24);
        Color darkStone = new(48, 52, 57);
        Color stone = new(76, 78, 79);
        Color edge = new(113, 108, 94);
        Color gold = new(143, 116, 62);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(coffin.X - 7, coffin.Bottom - 7, coffin.Width + 14, 9),
            shadow);
        _spriteBatch.Draw(_pixel, coffin, darkStone);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(coffin.X + 7, coffin.Y + 7, coffin.Width - 14, coffin.Height - 10),
            stone);
        _spriteBatch.Draw(_pixel, new Rectangle(coffin.X, coffin.Y, 5, coffin.Height), edge);
        _spriteBatch.Draw(_pixel, new Rectangle(coffin.Right - 5, coffin.Y, 5, coffin.Height), edge);
        _spriteBatch.Draw(_pixel, new Rectangle(coffin.X + 9, coffin.Bottom - 9, coffin.Width - 18, 3), edge);

        int centerX = coffin.Center.X;
        int centerY = coffin.Center.Y + 7;
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 13, centerY - 2, 26, 4), gold);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, centerY - 13, 4, 26), gold);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, centerY - 7, 14, 14), new Color(54, 58, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, centerY - 5, 4, 10), new Color(118, 178, 177));
    }

    private void DrawLid(Rectangle coffin, float progress)
    {
        int lift = (int)MathF.Round(progress * 28f);
        int slide = (int)MathF.Round(progress * 18f);
        Rectangle lid = new(
            coffin.X - 5 - slide,
            coffin.Y - 8 - lift,
            coffin.Width + 10,
            17);
        _spriteBatch.Draw(_pixel, new Rectangle(lid.X + 4, lid.Y + 5, lid.Width, lid.Height), new Color(17, 18, 21, 170));
        _spriteBatch.Draw(_pixel, lid, new Color(91, 91, 87));
        _spriteBatch.Draw(_pixel, new Rectangle(lid.X + 7, lid.Y + 3, lid.Width - 14, 3), new Color(135, 127, 103));
        _spriteBatch.Draw(_pixel, new Rectangle(lid.X + 16, lid.Y + 9, lid.Width - 32, 3), new Color(55, 58, 61));
    }

    private void DrawScholar(
        AncientScholarTombSystem tomb,
        GameTime gameTime)
    {
        float reveal = tomb.ScholarRevealProgress;
        int x = tomb.CoffinBounds.Center.X + 8;
        int restingY = tomb.CoffinBounds.Bottom - 7;
        int headY = (int)MathF.Round(MathHelper.Lerp(
            restingY + 18f,
            restingY - 63f,
            reveal));
        Color bone = new(176, 167, 137);
        Color oldBone = new(111, 106, 89);
        Color robe = new(45, 55, 72);
        Color robeEdge = new(73, 87, 103);
        Color gold = new(171, 132, 62);
        float eyePulse = .72f + .28f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 2.3f);
        Color eye = new(122, 218, 220, (int)(180 + 65 * eyePulse));

        // Torn scholar robes and seated torso.
        _spriteBatch.Draw(_pixel, new Rectangle(x - 19, headY + 22, 38, 42), robe);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 23, headY + 34, 7, 25), robeEdge);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 16, headY + 30, 8, 31), robeEdge);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 18, headY + 59, 9, 9), robe);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 4, headY + 56, 8, 13), robe);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 10, headY + 59, 7, 8), robe);

        // Very old skull with broken jaw and pale, non-hostile eyes.
        _spriteBatch.Draw(_pixel, new Rectangle(x - 13, headY, 26, 22), oldBone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 10, headY - 3, 20, 27), bone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 8, headY + 8, 6, 5), new Color(35, 38, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 3, headY + 8, 6, 5), new Color(35, 38, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 6, headY + 9, 3, 2), eye);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 4, headY + 9, 3, 2), eye);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 7, headY + 21, 15, 4), oldBone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 4, headY + 24, 3, 4), bone);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 3, headY + 24, 3, 3), bone);

        // Medallion, book, and scroll distinguish the scholar from enemies.
        _spriteBatch.Draw(_pixel, new Rectangle(x - 1, headY + 29, 2, 10), gold);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 5, headY + 38, 10, 9), gold);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 2, headY + 40, 4, 5), new Color(87, 140, 143));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 40, headY + 49, 26, 15), new Color(104, 65, 46));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 37, headY + 51, 10, 11), new Color(191, 173, 120));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 26, headY + 51, 10, 11), new Color(174, 154, 105));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 20, headY + 48, 27, 9), new Color(189, 170, 117));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 20, headY + 46, 4, 13), oldBone);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 43, headY + 46, 4, 13), oldBone);
    }

    private void DrawDust(
        AncientScholarTombSystem tomb,
        GameTime gameTime)
    {
        float progress = tomb.OpeningProgress;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        for (int index = 0; index < 14; index++)
        {
            float phase = index * 1.73f + time * .65f;
            int x = tomb.CoffinBounds.X + 7 + index * 9 +
                (int)(MathF.Sin(phase) * 8f);
            int y = tomb.CoffinBounds.Y + 20 -
                (int)(progress * (18f + index % 5 * 7f));
            int size = 2 + index % 3;
            int alpha = (int)(Math.Clamp(1f - progress * .72f, .2f, 1f) * 115f);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y, size, size),
                new Color(149, 137, 110, alpha));
        }
    }
}
