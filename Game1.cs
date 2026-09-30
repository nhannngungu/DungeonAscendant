using Microsoft.Xna.Framework;
using DungeonAscendant.Core;
using DungeonAscendant.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private GameSession _gameSession;
    private GameRenderer _renderer;

    public Game1()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _gameSession = new GameSession();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderer = new GameRenderer(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _gameSession.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.Draw();

        base.Draw(gameTime);
    }
}
