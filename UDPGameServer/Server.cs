using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UDPGameShared;

namespace UDPGameServer;

internal sealed class GameServer : IDisposable
{
    private const int Port = 1234;

    private readonly UdpClient _udp = new(Port);
    private readonly GameWorld _world = new();
    private readonly List<IPEndPoint> _clients = new();
    private readonly ConcurrentQueue<MovementInputMessage> _pendingInputs = new();
    private readonly object _clientsLock = new();
    private readonly object _sendLock = new();

    private IPEndPoint? _ballOwner;
    private volatile int _tickRate = 3;
    private volatile bool _running = true;

    public void Run()
    {
        Console.WriteLine($"Authoritative UDP server listening on port {Port}.");
        Console.WriteLine($"Tickrate: {_tickRate} Hz. Change it with: tick <1-120>");

        // The receive thread only receives, validates, and queues input.
        var receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();

        // The tick thread owns the world simulation and sends snapshots.
        var tickThread = new Thread(TickLoop) { IsBackground = true };
        tickThread.Start();

        while (_running)
        {
            string? command = Console.ReadLine();
            if (command is not null)
            {
                HandleCommand(command);
            }
        }
    }

    private void ReceiveLoop()
    {
        var sender = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                byte[] packet = _udp.Receive(ref sender);
                switch (MessageSerializer.Deserialize(packet))
                {
                    case JoinMessage:
                        Join(sender);
                        break;
                    case MovementInputMessage input
                        when IsBallOwner(sender) && input.ObjectId == GameWorld.BallId:
                        // Crossing the thread boundary is explicit: the receive
                        // thread queues input; the tick thread processes it.
                        _pendingInputs.Enqueue(input);
                        break;
                }
            }
            catch (SocketException) when (!_running)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private void TickLoop()
    {
        var tickTime = new Stopwatch();

        while (_running)
        {
            tickTime.Restart();
            Tick();

            double remainingMs = 1000d / _tickRate - tickTime.Elapsed.TotalMilliseconds;
            if (remainingMs > 0)
            {
                Thread.Sleep((int)remainingMs);
            }
        }
    }

    private void Join(IPEndPoint client)
    {
        bool ownsBall;
        lock (_clientsLock)
        {
            if (!_clients.Contains(client))
            {
                _clients.Add(client);
            }

            _ballOwner ??= client;
            ownsBall = client.Equals(_ballOwner);
        }

        Send(new JoinResultMessage
        {
            OwnedObjectId = ownsBall ? GameWorld.BallId : ""
        }, client);
        Send(new ServerSettingsMessage { TickRate = _tickRate }, client);
        Console.WriteLine($"{client} joined. Owns ball: {ownsBall}");
    }

    private bool IsBallOwner(IPEndPoint client)
    {
        lock (_clientsLock)
        {
            return client.Equals(_ballOwner);
        }
    }

    private void Tick()
    {
        // The authoritative world only changes on the fixed server tick.
        while (_pendingInputs.TryDequeue(out MovementInputMessage? input))
        {
            _world.ApplyInput(input);
        }

        ObjectSnapshotMessage snapshot = _world.CreateSnapshot();
        IPEndPoint[] recipients;
        lock (_clientsLock)
        {
            recipients = _clients.ToArray();
        }

        foreach (IPEndPoint client in recipients)
        {
            Send(snapshot, client);
        }
    }

    private void Send(INetworkMessage message, IPEndPoint client)
    {
        byte[] packet = MessageSerializer.Serialize(message);

        // Join replies and snapshots may be sent by different threads.
        lock (_sendLock)
        {
            _udp.Send(packet, packet.Length, client);
        }
    }

    private void HandleCommand(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            parts[0].Equals("tick", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(parts[1], out int tickRate) &&
            tickRate is >= 1 and <= 120)
        {
            _tickRate = tickRate;
            Console.WriteLine($"Tickrate changed to {_tickRate} Hz.");
            Broadcast(new ServerSettingsMessage { TickRate = _tickRate });
        }
        else
        {
            Console.WriteLine("Use: tick <1-120>");
        }
    }

    private void Broadcast(INetworkMessage message)
    {
        IPEndPoint[] recipients;
        lock (_clientsLock)
        {
            recipients = _clients.ToArray();
        }

        foreach (IPEndPoint client in recipients)
        {
            Send(message, client);
        }
    }

    public void Dispose()
    {
        _running = false;
        _udp.Dispose();
    }
}
