using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UDPGameShared;

namespace UDPGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly DemoSettings _settings = new();
    private readonly ConcurrentQueue<INetworkMessage> _incomingMessages = new();

    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private UDPGameClient _client = null!;
    private Ball _ball = null!;
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private int _framesThisSecond;
    private int _clientFramesPerSecond;
    private double _frameTimer;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Rendering is independent of the server tickrate and runs as fast as
        // the machine allows. NetworkGameObject still sends input at 60 Hz.
        IsFixedTimeStep = false;
        _graphics.SynchronizeWithVerticalRetrace = false;
    }

    protected override void Initialize()
    {
        _client = new UDPGameClient(
            message => _incomingMessages.Enqueue(message),
            () => _settings.SimulatedOneWayLatencyMs);

        var startPosition = new Vector2(
            GraphicsDevice.Viewport.Width / 2f,
            GraphicsDevice.Viewport.Height / 2f);

        _ball = new Ball(startPosition, _client, _settings);
        _client.Send(new JoinMessage());
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("font");
        _ball.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();

        if (_currentKeyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        HandleSettingsInput();
        HandleIncomingMessages();
        _ball.Update(gameTime, _currentKeyboard);

        base.Update(gameTime);
    }

    private void HandleSettingsInput()
    {
        if (WasPressed(Keys.P))
        {
            _settings.PredictionEnabled = !_settings.PredictionEnabled;
        }

        if (WasPressed(Keys.R))
        {
            _settings.ReconciliationEnabled = !_settings.ReconciliationEnabled;
        }

        if (WasPressed(Keys.I))
        {
            _settings.InterpolationEnabled = !_settings.InterpolationEnabled;
        }

        if (WasPressed(Keys.OemPlus) || WasPressed(Keys.Add))
        {
            _settings.SimulatedOneWayLatencyMs += 25;
        }

        if (WasPressed(Keys.OemMinus) || WasPressed(Keys.Subtract))
        {
            _settings.SimulatedOneWayLatencyMs = Math.Max(
                0,
                _settings.SimulatedOneWayLatencyMs - 25);
        }
    }

    private bool WasPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    private void HandleIncomingMessages()
    {
        while (_incomingMessages.TryDequeue(out INetworkMessage? message))
        {
            switch (message)
            {
                case JoinResultMessage joinResult:
                    _ball.IsOwnedByThisClient = joinResult.OwnedObjectId == _ball.ObjectId;
                    break;
                case ObjectSnapshotMessage snapshot:
                    _ball.HandleSnapshot(snapshot);
                    break;
                case ServerSettingsMessage serverSettings:
                    _settings.ServerTickRate = serverSettings.TickRate;
                    break;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        UpdateFrameCounter(gameTime);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        _ball.Draw(_spriteBatch);
        _spriteBatch.DrawString(_font, BuildStatusText(), Vector2.Zero, Color.Black);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateFrameCounter(GameTime gameTime)
    {
        _framesThisSecond++;
        _frameTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (_frameTimer >= 1d)
        {
            _clientFramesPerSecond = _framesThisSecond;
            _framesThisSecond = 0;
            _frameTimer -= 1d;
        }
    }

    private string BuildStatusText()
    {
        string ownership = _ball.IsOwnedByThisClient
            ? "You own the ball - hold W/A/S/D to move"
            : "You observe the server-owned ball";

        return $"{ownership}\n" +
               $"Client rendering: {_clientFramesPerSecond} FPS\n" +
               $"Server tickrate: {_settings.ServerTickRate} Hz (server command: tick <hz>)\n" +
               $"Simulated latency: {_settings.SimulatedOneWayLatencyMs} ms each way " +
               $"(~{_settings.SimulatedOneWayLatencyMs * 2} ms RTT) [+/-]\n" +
               $"Prediction: {_settings.PredictionEnabled} [P]\n" +
               $"Reconciliation: {_settings.ReconciliationEnabled} [R]\n" +
               $"Interpolation: {_settings.InterpolationEnabled} [I]";
    }

    protected override void UnloadContent()
    {
        _client.Dispose();
        base.UnloadContent();
    }
}
