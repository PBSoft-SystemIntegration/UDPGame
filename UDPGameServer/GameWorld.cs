using UDPGameShared;

namespace UDPGameServer;

internal sealed class GameWorld
{
    private const float MoveSpeed = 4f;
    public const string BallId = "ball";
    private float _ballX = 400f;
    private float _ballY = 240f;
    private int _lastProcessedInput;
    private int _snapshotSequence;

    public void ApplyInput(MovementInputMessage input)
    {
        if (input.Sequence <= _lastProcessedInput)
        {
            return; // Ignore old or duplicate UDP input.
        }

        _ballX += input.DirectionX * MoveSpeed;
        _ballY += input.DirectionY * MoveSpeed;
        _lastProcessedInput = input.Sequence;
    }

    public ObjectSnapshotMessage CreateSnapshot()
    {
        return new ObjectSnapshotMessage
        {
            ObjectId = BallId,
            SnapshotSequence = ++_snapshotSequence,
            LastProcessedInput = _lastProcessedInput,
            PositionX = _ballX,
            PositionY = _ballY
        };
    }
}
