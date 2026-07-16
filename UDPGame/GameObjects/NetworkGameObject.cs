using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using UDPGameShared;

namespace UDPGame;

// Shared client behaviour for every networked object. A locally owned object
// uses prediction and reconciliation. A remote/server-owned object uses
// interpolation. Ball only has to decide which movement input to produce.
public abstract class NetworkGameObject
{
    private const double ClientInputInterval = 1d / 60d;

    private readonly string _assetName;
    private readonly float _moveSpeed;
    private readonly UDPGameClient _client;
    private readonly DemoSettings _settings;
    private readonly List<MovementInputMessage> _unprocessedInputs = new();
    private readonly List<(DateTime time, Vector2 position)> _positionBuffer = new();

    private Texture2D _texture = null!;
    private int _inputSequence;
    private int _lastSnapshotSequence = -1;
    private double _inputTimer;

    protected NetworkGameObject(
        string objectId,
        string assetName,
        Vector2 startPosition,
        float moveSpeed,
        UDPGameClient client,
        DemoSettings settings)
    {
        ObjectId = objectId;
        _assetName = assetName;
        Position = startPosition;
        _moveSpeed = moveSpeed;
        _client = client;
        _settings = settings;
    }

    public string ObjectId { get; }
    public Vector2 Position { get; private set; }
    public bool IsOwnedByThisClient { get; set; }

    public void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>(_assetName);
    }

    // Local input runs at a stable 60 Hz even when rendering is much faster.
    // Otherwise movement speed and packet count would depend on client FPS.
    protected void UpdateOwned(GameTime gameTime, Vector2 direction)
    {
        _inputTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (direction == Vector2.Zero)
        {
            // Do not build up hundreds of delayed inputs while standing still.
            _inputTimer = Math.Min(_inputTimer, ClientInputInterval);
            return;
        }

        while (_inputTimer >= ClientInputInterval)
        {
            SendMovement(direction.X, direction.Y);
            _inputTimer -= ClientInputInterval;
        }
    }

    // Remote and server-owned objects have no local input to send.
    protected void UpdateRemote()
    {
        if (_settings.InterpolationEnabled)
        {
            Interpolate();
        }
    }

    private void SendMovement(float directionX, float directionY)
    {
        var input = new MovementInputMessage
        {
            ObjectId = ObjectId,
            Sequence = ++_inputSequence,
            DirectionX = directionX,
            DirectionY = directionY
        };

        if (_settings.PredictionEnabled)
        {
            // CLIENT-SIDE PREDICTION:
            // Apply our input immediately instead of waiting for the RTT.
            ApplyMovement(input);
            _unprocessedInputs.Add(input);
        }

        _client.Send(input);
    }

    public void HandleSnapshot(ObjectSnapshotMessage snapshot)
    {
        if (snapshot.ObjectId != ObjectId)
        {
            return;
        }

        if (snapshot.SnapshotSequence <= _lastSnapshotSequence)
        {
            // UDP may reorder or duplicate packets. A newer complete snapshot
            // makes every older snapshot irrelevant.
            return;
        }

        _lastSnapshotSequence = snapshot.SnapshotSequence;

        if (IsOwnedByThisClient)
        {
            Reconcile(snapshot);
        }
        else if (_settings.InterpolationEnabled)
        {
            // REMOTE OBJECT / NPC:
            // Buffer server positions instead of snapping on packet arrival.
            _positionBuffer.Add((DateTime.UtcNow, SnapshotPosition(snapshot)));
        }
        else
        {
            Position = SnapshotPosition(snapshot);
            _positionBuffer.Clear();
        }
    }

    private void Reconcile(ObjectSnapshotMessage snapshot)
    {
        // 1. Start at the authoritative server position.
        Position = SnapshotPosition(snapshot);

        // 2. Remove inputs acknowledged by the server snapshot.
        _unprocessedInputs.RemoveAll(
            input => input.Sequence <= snapshot.LastProcessedInput);

        if (_settings.PredictionEnabled && _settings.ReconciliationEnabled)
        {
            // 3. Replay input still in flight. This moves the object from the
            // server's confirmed past back to the client's predicted present.
            foreach (MovementInputMessage input in _unprocessedInputs)
            {
                ApplyMovement(input);
            }
        }
    }

    private void Interpolate()
    {
        if (_positionBuffer.Count == 1)
        {
            Position = _positionBuffer[0].position;
            return;
        }

        // Render one server tick in the past, where two known snapshots are
        // normally available. This avoids guessing a remote object's future.
        DateTime renderTime = DateTime.UtcNow -
            TimeSpan.FromSeconds(1d / _settings.ServerTickRate);

        while (_positionBuffer.Count >= 2 &&
               _positionBuffer[1].time <= renderTime)
        {
            _positionBuffer.RemoveAt(0);
        }

        if (_positionBuffer.Count < 2 ||
            renderTime < _positionBuffer[0].time ||
            renderTime > _positionBuffer[1].time)
        {
            return;
        }

        var from = _positionBuffer[0];
        var to = _positionBuffer[1];
        double duration = (to.time - from.time).TotalMilliseconds;
        if (duration <= 0)
        {
            Position = to.position;
            return;
        }

        float amount = (float)((renderTime - from.time).TotalMilliseconds / duration);
        Position = Vector2.Lerp(from.position, to.position, amount);
    }

    // The authoritative server applies the same movement rule.
    private void ApplyMovement(MovementInputMessage input)
    {
        Position += new Vector2(input.DirectionX, input.DirectionY) * _moveSpeed;
    }

    private static Vector2 SnapshotPosition(ObjectSnapshotMessage snapshot)
    {
        return new Vector2(snapshot.PositionX, snapshot.PositionY);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
        spriteBatch.Draw(_texture, Position, null, Color.White, 0f, origin,
            Vector2.One, SpriteEffects.None, 0f);
    }
}
