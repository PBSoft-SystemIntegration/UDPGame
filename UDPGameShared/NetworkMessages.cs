namespace UDPGameShared;

public enum MessageType : byte
{
    Join,
    MovementInput,
    JoinResult,
    ObjectSnapshot,
    ServerSettings
}

public interface INetworkMessage { }

public sealed class JoinMessage : INetworkMessage { }

public sealed class MovementInputMessage : INetworkMessage
{
    public string ObjectId { get; set; } = "";
    public int Sequence { get; set; }
    public float DirectionX { get; set; }
    public float DirectionY { get; set; }
}

public sealed class JoinResultMessage : INetworkMessage
{
    public string OwnedObjectId { get; set; } = "";
}

public sealed class ObjectSnapshotMessage : INetworkMessage
{
    public string ObjectId { get; set; } = "";
    public int SnapshotSequence { get; set; }
    public int LastProcessedInput { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
}

public sealed class ServerSettingsMessage : INetworkMessage
{
    public int TickRate { get; set; }
}
