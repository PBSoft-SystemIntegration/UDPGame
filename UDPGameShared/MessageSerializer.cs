using System.Text.Json;

namespace UDPGameShared;

public static class MessageSerializer
{
    // UDP packet format:
    // [one byte message type] [JSON containing only that message's fields]
    public static byte[] Serialize(INetworkMessage message)
    {
        switch (message)
        {
            case JoinMessage:
                return CreatePacket(MessageType.Join, Array.Empty<byte>());

            case MovementInputMessage input:
                return CreatePacket(
                    MessageType.MovementInput,
                    JsonSerializer.SerializeToUtf8Bytes(input));

            case JoinResultMessage joinResult:
                return CreatePacket(
                    MessageType.JoinResult,
                    JsonSerializer.SerializeToUtf8Bytes(joinResult));

            case ObjectSnapshotMessage snapshot:
                return CreatePacket(
                    MessageType.ObjectSnapshot,
                    JsonSerializer.SerializeToUtf8Bytes(snapshot));

            case ServerSettingsMessage settings:
                return CreatePacket(
                    MessageType.ServerSettings,
                    JsonSerializer.SerializeToUtf8Bytes(settings));

            default:
                throw new ArgumentException("Unknown message type.");
        }
    }

    public static INetworkMessage Deserialize(byte[] packet)
    {
        if (packet.Length == 0)
        {
            throw new ArgumentException("The packet is empty.");
        }

        MessageType type = (MessageType)packet[0];
        ReadOnlySpan<byte> json = packet.AsSpan(1);

        switch (type)
        {
            case MessageType.Join:
                return new JoinMessage();

            case MessageType.MovementInput:
                return JsonSerializer.Deserialize<MovementInputMessage>(json)!;

            case MessageType.JoinResult:
                return JsonSerializer.Deserialize<JoinResultMessage>(json)!;

            case MessageType.ObjectSnapshot:
                return JsonSerializer.Deserialize<ObjectSnapshotMessage>(json)!;

            case MessageType.ServerSettings:
                return JsonSerializer.Deserialize<ServerSettingsMessage>(json)!;

            default:
                throw new ArgumentException("Unknown message type.");
        }
    }

    private static byte[] CreatePacket(MessageType type, byte[] json)
    {
        var packet = new byte[json.Length + 1];
        packet[0] = (byte)type;
        json.CopyTo(packet, 1);
        return packet;
    }
}
