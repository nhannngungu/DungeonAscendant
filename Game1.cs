using Microsoft.Xna.Framework;
using DungeonAscendant.Core;
using DungeonAscendant.Audio;
using DungeonAscendant.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private GameSession _gameSession;
    private GameRenderer _renderer;
    private AudioManager _audioManager;
    private CombatAudioDirector _combatAudio;

    public Game1()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Viewport viewport = GraphicsDevice.Viewport;
        var viewportBounds = new Rectangle(0, 0, viewport.Width, viewport.Height);
        _gameSession = new GameSession(viewportBounds);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _renderer = new GameRenderer(GraphicsDevice);
        _audioManager = new AudioManager(Content);
        _combatAudio = new CombatAudioDirector(_audioManager);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        _gameSession.Update(gameTime, keyboardState, mouseState);
        _combatAudio.Update(_gameSession, gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.Draw(_gameSession, gameTime);

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _gameSession?.FlushActiveSave();
        _audioManager?.Dispose();
        _renderer.Dispose();
        base.UnloadContent();
    }
}
