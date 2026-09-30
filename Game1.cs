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
        Viewport viewport = GraphicsDevice.Viewport;
        _gameSession = new GameSession(new Vector2(viewport.Width / 2f, viewport.Height / 2f));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderer = new GameRenderer(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        _gameSession.Update(gameTime, keyboardState);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.Draw(_gameSession);

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _renderer.Dispose();
        base.UnloadContent();
    }
}
