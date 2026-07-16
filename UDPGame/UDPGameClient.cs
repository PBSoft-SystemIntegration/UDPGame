using System.Net;
using System.Net.Sockets;
using UDPGameShared;

namespace UDPGame;

public sealed class UDPGameClient : IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly Action<INetworkMessage> _onMessage;
    private readonly Func<int> _getLatencyMs;
    private bool _running = true;

    public UDPGameClient(Action<INetworkMessage> onMessage, Func<int> getLatencyMs)
    {
        _onMessage = onMessage;
        _getLatencyMs = getLatencyMs;
        _udpClient.Connect(IPAddress.Loopback.ToString(), 1234);

        var receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
    }

    public async void Send(INetworkMessage message)
    {
        try
        {
            // One-way latency from client to server.
            await Task.Delay(_getLatencyMs());
            byte[] bytes = MessageSerializer.Serialize(message);
            _udpClient.Send(bytes, bytes.Length);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void ReceiveLoop()
    {
        var server = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                byte[] bytes = _udpClient.Receive(ref server);
                DeliverAfterLatency(bytes);
            }
            catch (SocketException) when (!_running)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async void DeliverAfterLatency(byte[] bytes)
    {
        // The same one-way latency is also applied from server to client.
        // The simulated round-trip time is therefore roughly latency * 2.
        await Task.Delay(_getLatencyMs());
        if (_running)
        {
            _onMessage(MessageSerializer.Deserialize(bytes));
        }
    }

    public void Dispose()
    {
        _running = false;
        _udpClient.Dispose();
    }
}
