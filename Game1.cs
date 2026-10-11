using Microsoft.Xna.Framework;
using DungeonAscendant.Core;
using DungeonAscendant.Audio;
using DungeonAscendant.Graphics;
using DungeonAscendant.Localization;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant;

public class Game1 : Game
{
    private const int UpdateDiagnosticFrameLimit = 5;
    private const int DrawDiagnosticFrameLimit = 5;
    private const double ControllerQuitGuardSeconds = 3d;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private GameSession _gameSession;
    private GameRenderer _renderer;
    private AudioManager _audioManager;
    private CombatAudioDirector _combatAudio;
    private KeyboardState _previousKeyboardState;
    private bool _applyingDisplaySettings;
    private GameDisplayMode _appliedDisplayMode;
    private int _appliedWindowWidth;
    private int _appliedWindowHeight;
    private int _updateFrame;
    private int _drawFrame;
    private KeyboardState _lastDiagnosticKeyboard;
    private GamePadState _lastDiagnosticGamePad;
    private bool _controllerGuardLogged;

    public Game1()
    {
        StartupLog.Write("Game1 constructor started.");
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Exiting += HandleGameExiting;
        Activated += HandleActivated;
        Deactivated += HandleDeactivated;
        try
        {
            ConfigureGraphicsFromSettings();
        }
        catch (System.Exception exception)
        {
            StartupLog.Exception(
                "Initial graphics preference configuration failed; using safe windowed defaults",
                exception);
            ConfigureSafeWindowedGraphics();
        }
        StartupLog.Write(
            $"Game1 constructed; settings loaded: language={Loc.CurrentLanguage}, " +
            $"display={Loc.DisplayMode}, window={Loc.WindowWidth}x{Loc.WindowHeight}.");
        StartupLog.Write($"Graphics backend requested: {GraphicsBackendName}.");
        StartupLog.Write(
            $"Diagnostic controller-quit guard enabled for first " +
            $"{ControllerQuitGuardSeconds:0} seconds; controller input has no direct application-exit binding.");
    }

    protected override void Initialize()
    {
        StartupLog.Write("Initialize started.");
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += HandleClientSizeChanged;
        ApplyDisplaySettings(force: true);
        StartupLog.Write(
            $"Graphics configured: viewport={GraphicsDevice.Viewport.Width}x" +
            $"{GraphicsDevice.Viewport.Height}, mode={Loc.DisplayMode}, " +
            $"backend={GraphicsBackendName}, profile={GraphicsDevice.GraphicsProfile}, " +
            $"adapter={GraphicsAdapter.DefaultAdapter.Description}.");
        Viewport viewport = GraphicsDevice.Viewport;
        var viewportBounds = new Rectangle(0, 0, viewport.Width, viewport.Height);
        _gameSession = new GameSession(viewportBounds);
        StartupLog.Write(
            $"Save slots loaded; occupied={CountOccupiedSlots()}, error=" +
            $"{(string.IsNullOrEmpty(_gameSession.SaveError) ? "none" : _gameSession.SaveError)}.");
        StartupLog.Write(
            $"Localization loaded: {Loc.CurrentLanguage}; frontend state selected: " +
            $"{_gameSession.State}.");

        base.Initialize();
        StartupLog.Write("Initialize completed.");
    }

    protected override void LoadContent()
    {
        StartupLog.Write("LoadContent started.");
        _renderer = new GameRenderer(GraphicsDevice);
        try
        {
            _audioManager = new AudioManager(Content);
            StartupLog.Write(
                $"Audio initialized; missing optional assets={_audioManager.MissingAssets.Count}.");
        }
        catch (System.Exception exception)
        {
            StartupLog.Exception(
                "Audio initialization failed; continuing with audio disabled",
                exception);
            _audioManager = AudioManager.CreateDisabled();
        }
        _combatAudio = new CombatAudioDirector(_audioManager);
        StartupLog.Write("LoadContent completed.");
    }

    protected override void Update(GameTime gameTime)
    {
        _updateFrame++;
        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
        _lastDiagnosticKeyboard = keyboardState;
        _lastDiagnosticGamePad = gamePadState;

        if (_updateFrame == 1)
            LogGamePadCapabilities();
        if (_updateFrame <= UpdateDiagnosticFrameLimit)
            StartupLog.Write(
                $"[FRAME] Update {_updateFrame}; state={CurrentState}; active={IsActive}; " +
                $"keyboard={FormatKeyboard(keyboardState)}; " +
                $"gamepad={FormatGamePad(gamePadState)}");

        if (gameTime.TotalGameTime.TotalSeconds < ControllerQuitGuardSeconds &&
            gamePadState.Buttons.Back == ButtonState.Pressed &&
            !_controllerGuardLogged)
        {
            _controllerGuardLogged = true;
            StartupLog.Write(
                $"[DIAGNOSTIC GUARD] Controller Back observed during first " +
                $"{ControllerQuitGuardSeconds:0} seconds; application quit is suppressed.");
        }

        bool altDown = keyboardState.IsKeyDown(Keys.LeftAlt) ||
            keyboardState.IsKeyDown(Keys.RightAlt);
        bool toggleDisplayPressed = altDown &&
            keyboardState.IsKeyDown(Keys.Enter) &&
            !_previousKeyboardState.IsKeyDown(Keys.Enter);
        if (toggleDisplayPressed)
        {
            Loc.SetDisplayMode(Loc.DisplayMode == GameDisplayMode.Windowed
                ? GameDisplayMode.Fullscreen
                : GameDisplayMode.Windowed);
        }

        _gameSession.Update(gameTime, keyboardState, mouseState);
        ApplyDisplaySettings(force: false);
        _combatAudio.Update(_gameSession, gameTime);
        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _drawFrame++;
        bool diagnoseDraw = _drawFrame <= DrawDiagnosticFrameLimit;
        if (diagnoseDraw)
        {
            PresentationParameters presentation = GraphicsDevice.PresentationParameters;
            StartupLog.Write($"[DRAW] Begin frame {_drawFrame}");
            StartupLog.Write(
                $"[DRAW] Presentation state frame {_drawFrame}: state={CurrentState}; " +
                $"active={IsActive}; viewport={GraphicsDevice.Viewport.Width}x" +
                $"{GraphicsDevice.Viewport.Height}; backbuffer=" +
                $"{presentation.BackBufferWidth}x{presentation.BackBufferHeight}; " +
                $"format={presentation.BackBufferFormat}; depth=" +
                $"{presentation.DepthStencilFormat}; fullscreen=" +
                $"{presentation.IsFullScreen}; renderTargets=" +
                $"{GraphicsDevice.GetRenderTargets().Length}");
        }

        _renderer.Draw(
            _gameSession,
            gameTime,
            diagnoseDraw ? StartupLog.Write : null);

        base.Draw(gameTime);
        if (diagnoseDraw)
        {
            StartupLog.Write($"[DRAW] Base Draw completed frame {_drawFrame}");
            StartupLog.Write($"[DRAW] End frame {_drawFrame}");
            if (_drawFrame == 1)
                StartupLog.Write(
                    $"First completed Draw; backend={GraphicsBackendName}, " +
                    $"adapter={GraphicsAdapter.DefaultAdapter.Description}, " +
                    $"resolution={GraphicsDevice.Viewport.Width}x" +
                    $"{GraphicsDevice.Viewport.Height}.");
        }
    }

    protected override void UnloadContent()
    {
        StartupLog.Write("UnloadContent started.");
        _gameSession?.FlushActiveSave();
        _audioManager?.Dispose();
        _renderer?.Dispose();
        base.UnloadContent();
        StartupLog.Write("UnloadContent completed.");
    }

    private void HandleGameExiting(object sender, System.EventArgs args)
    {
        StartupLog.Write(
            $"[EXITING] frame={_updateFrame}; draw={_drawFrame}; state={CurrentState}; " +
            $"IsActive={IsActive}; keyboard={FormatKeyboard(_lastDiagnosticKeyboard)}; " +
            $"gamepad={FormatGamePad(_lastDiagnosticGamePad)}; window={FormatWindow()}; " +
            $"reason=MonoGame platform/window lifecycle requested shutdown.");
    }

    private void HandleActivated(object sender, System.EventArgs args) =>
        StartupLog.Write(
            $"[WINDOW] Activated; frame={_updateFrame}; state={CurrentState}; {FormatWindow()}");

    private void HandleDeactivated(object sender, System.EventArgs args) =>
        StartupLog.Write(
            $"[WINDOW] Deactivated; frame={_updateFrame}; state={CurrentState}; {FormatWindow()}");

    private void ConfigureGraphicsFromSettings()
    {
        (int desktopWidth, int desktopHeight) = GetDesktopResolution();
        bool windowed = Loc.DisplayMode == GameDisplayMode.Windowed;
        _graphicsDeviceManager.HardwareModeSwitch =
            Loc.DisplayMode == GameDisplayMode.Fullscreen;
        _graphicsDeviceManager.IsFullScreen = !windowed;
        _graphicsDeviceManager.PreferredBackBufferWidth = windowed
            ? System.Math.Min(Loc.WindowWidth, desktopWidth)
            : desktopWidth;
        _graphicsDeviceManager.PreferredBackBufferHeight = windowed
            ? System.Math.Min(Loc.WindowHeight, desktopHeight)
            : desktopHeight;
    }

    private void ApplyDisplaySettings(bool force)
    {
        bool windowResolutionChanged = Loc.DisplayMode == GameDisplayMode.Windowed &&
            (Loc.WindowWidth != _appliedWindowWidth ||
                Loc.WindowHeight != _appliedWindowHeight);
        if (!force && Loc.DisplayMode == _appliedDisplayMode &&
            !windowResolutionChanged)
        {
            return;
        }

        _applyingDisplaySettings = true;
        try
        {
            ConfigureGraphicsFromSettings();
            _graphicsDeviceManager.ApplyChanges();
            _appliedDisplayMode = Loc.DisplayMode;
            _appliedWindowWidth = Loc.WindowWidth;
            _appliedWindowHeight = Loc.WindowHeight;
            ResizeSessionToClient();
        }
        catch (System.Exception exception)
        {
            StartupLog.Exception(
                "Display mode application failed; falling back to Windowed 1280x720",
                exception);
            Loc.SetDisplayMode(GameDisplayMode.Windowed);
            Loc.SetWindowResolution(1280, 720);
            ConfigureSafeWindowedGraphics();
            _graphicsDeviceManager.ApplyChanges();
            _appliedDisplayMode = GameDisplayMode.Windowed;
            _appliedWindowWidth = 1280;
            _appliedWindowHeight = 720;
            ResizeSessionToClient();
            StartupLog.Write("Safe Windowed display fallback applied successfully.");
        }
        finally
        {
            _applyingDisplaySettings = false;
        }
    }

    private void HandleClientSizeChanged(object sender, System.EventArgs eventArgs)
    {
        StartupLog.Write(
            $"[WINDOW] ClientSizeChanged; frame={_updateFrame}; {FormatWindow()}");
        ResizeSessionToClient();
        if (_applyingDisplaySettings || Loc.DisplayMode != GameDisplayMode.Windowed)
            return;
        Rectangle bounds = Window.ClientBounds;
        if (bounds.Width < 800 || bounds.Height < 480)
            return;
        Loc.SetWindowResolution(bounds.Width, bounds.Height);
        _appliedWindowWidth = Loc.WindowWidth;
        _appliedWindowHeight = Loc.WindowHeight;
    }

    private void ResizeSessionToClient()
    {
        Rectangle bounds = Window.ClientBounds;
        if (bounds.Width > 0 && bounds.Height > 0)
            _gameSession?.ResizeViewport(new Rectangle(0, 0, bounds.Width, bounds.Height));
    }

    private void ConfigureSafeWindowedGraphics()
    {
        int width = 1280;
        int height = 720;
        try
        {
            (int desktopWidth, int desktopHeight) = GetDesktopResolution();
            width = System.Math.Min(width, desktopWidth);
            height = System.Math.Min(height, desktopHeight);
        }
        catch (System.Exception exception)
        {
            StartupLog.Exception(
                "Desktop resolution query failed while applying safe fallback",
                exception);
        }
        _graphicsDeviceManager.HardwareModeSwitch = false;
        _graphicsDeviceManager.IsFullScreen = false;
        _graphicsDeviceManager.PreferredBackBufferWidth = System.Math.Max(640, width);
        _graphicsDeviceManager.PreferredBackBufferHeight = System.Math.Max(360, height);
    }

    private static (int Width, int Height) GetDesktopResolution()
    {
        DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        if (desktop.Width < 640 || desktop.Height < 360)
            throw new System.InvalidOperationException(
                $"Invalid desktop display mode {desktop.Width}x{desktop.Height}.");
        return (desktop.Width, desktop.Height);
    }

    private int CountOccupiedSlots()
    {
        int occupied = 0;
        foreach (var slot in _gameSession.SaveSlots)
            if (slot.Occupied) occupied++;
        return occupied;
    }

    private string CurrentState => _gameSession == null
        ? "not-created"
        : _gameSession.State.ToString();

    private string FormatWindow()
    {
        Rectangle bounds = Window.ClientBounds;
        string viewport = GraphicsDevice == null
            ? "unavailable"
            : $"{GraphicsDevice.Viewport.Width}x{GraphicsDevice.Viewport.Height}";
        return $"client={bounds.Width}x{bounds.Height}@{bounds.X},{bounds.Y}; " +
            $"viewport={viewport}; display={Loc.DisplayMode}; configured=" +
            $"{_graphicsDeviceManager.PreferredBackBufferWidth}x" +
            $"{_graphicsDeviceManager.PreferredBackBufferHeight}";
    }

    private static string FormatKeyboard(KeyboardState keyboard) =>
        $"Escape={Down(keyboard, Keys.Escape)},Alt=" +
        $"{(Down(keyboard, Keys.LeftAlt) || Down(keyboard, Keys.RightAlt))}," +
        $"F4={Down(keyboard, Keys.F4)},Enter={Down(keyboard, Keys.Enter)}," +
        $"Backspace={Down(keyboard, Keys.Back)}";

    private static string FormatGamePad(GamePadState gamePad) =>
        $"Connected={gamePad.IsConnected},Back=" +
        $"{gamePad.Buttons.Back == ButtonState.Pressed},Start=" +
        $"{gamePad.Buttons.Start == ButtonState.Pressed},Guide=" +
        $"{gamePad.Buttons.BigButton == ButtonState.Pressed}";

    private static bool Down(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key);

    private static string GraphicsBackendName
    {
        get
        {
#if WINDOWS_DX
            return "MonoGame WindowsDX";
#else
            return "MonoGame DesktopGL";
#endif
        }
    }

    private static void LogGamePadCapabilities()
    {
        try
        {
            GamePadCapabilities capabilities = GamePad.GetCapabilities(PlayerIndex.One);
            StartupLog.Write(
                $"[INPUT] GamePad capabilities: connected={capabilities.IsConnected}, " +
                $"type={capabilities.GamePadType}, name={capabilities.DisplayName ?? "n/a"}, " +
                $"identifier={capabilities.Identifier ?? "n/a"}, Back=" +
                $"{capabilities.HasBackButton}, Start={capabilities.HasStartButton}, " +
                $"Guide={capabilities.HasBigButton}.");
        }
        catch (System.Exception exception)
        {
            StartupLog.Exception("[INPUT] GamePad capability query failed", exception);
        }
    }
}
