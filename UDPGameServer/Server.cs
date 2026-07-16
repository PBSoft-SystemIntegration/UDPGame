using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace UDPGameServer
{
    internal class Server
    {
        private System.Timers.Timer timer;
        IPEndPoint clientEndPoint;
        UdpClient udpServer;
        GameWorld world;
        float snapshotSpeed = 3;
        List<IPEndPoint> clients = new List<IPEndPoint>();
        private CommandExecutor commandExecutor = new CommandExecutor();
        public Server()
        {
            commandExecutor.RegisterCommand("tick", SetTickRate);          
            timer = new System.Timers.Timer();
            timer.Interval = 1000f / snapshotSpeed;
            timer.Elapsed += TimerElapsed;
            udpServer = new UdpClient(1234);
            Console.WriteLine("server stated Listening on UDP port 1234");
            Console.WriteLine($"Setting tick rate to {snapshotSpeed}");
            Thread recieveThread = new Thread(RecieiveThread);
            recieveThread.Start();
            Start();
            world = new GameWorld();
            Thread inputThread = new Thread(ReadInput);
            inputThread.Start();
        }

        private void ReadInput()
        {
            while (true)
            {
               var input = Console.ReadLine();

                commandExecutor.ExecuteCommand(input);
            }
        }
        private void SetTickRate(string[] args)
        {
            if (args.Length == 1 && int.TryParse(args[0], out int tickRate))
            {
                Console.WriteLine($"Setting tick rate to {tickRate}");

                float snapshotSpeed = tickRate;
                timer.Interval = 1000f / snapshotSpeed;
            }
            else
            {
                Console.WriteLine("Invalid arguments for tick");
            }
        }
        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }
        //recieving
        void RecieiveThread()
        {
            //listening as fast as possible!
            while (true)
            {
                try
                {

                    byte[] receivedData = udpServer.Receive(ref clientEndPoint);
                    if (!clients.Contains(clientEndPoint))
                    {
                        clients.Add(clientEndPoint);
                    }
                    //reacting to client input.
                    HandleMessageRecieved(receivedData, clientEndPoint);
                }
                catch (Exception)
                {
                    Console.WriteLine("seems like somehting left unexpected!");
                }
                clientEndPoint = null;
            }
        }

        //sending. called snapshotSpeed times a second.
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (clients.Count == 0)
            {
                return;
            }
             foreach (var client in clients)
            {
                SendDataToClient(world.GetWorldStateSnapShot(), client);
            }
        }
        public void SendDataToClient(NetworkMessage message, IPEndPoint ep)
        {
            byte[] messageBytes = new byte[1024];
            byte messageTypeByte = message.GetMessageTypeAsByte;
            switch (message.MessageType)
            {
                case MessageType.SnapShot:
                    messageBytes = MessagePackSerializer.Serialize((SnapShot)message);
                    break;
                case MessageType.JoinAnswer:
                    messageBytes = MessagePackSerializer.Serialize((JoinAnswer)message);
                    break;
                default:
                    break;
            }
            byte[] combinedBytes = new byte[1 + messageBytes.Length];
            combinedBytes[0] = messageTypeByte;
            Buffer.BlockCopy(messageBytes, 0, combinedBytes, 1, messageBytes.Length);
            udpServer.Send(combinedBytes, ep);
        }
        void HandleMessageRecieved(byte[] receivedData, IPEndPoint clientEP)
        {
            try
            {
                MessageType messageType = (MessageType)receivedData[0];
                byte[] dataToDeserialize = receivedData.Skip(1).ToArray();
                switch (messageType)
                {
                    case MessageType.MovementUpdate:
                        MovementUpdate mov = MessagePackSerializer.Deserialize<MovementUpdate>(dataToDeserialize);
                        world.UpdateBallMovement(mov);
                        break;
                    case MessageType.Join:
                        //the first joiner is the owner of the ball.
                        SendDataToClient(new JoinAnswer { BallOwner =(clients.Count == 1)}, clientEP);

                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("dang yo code bad");
                throw;
            }
        }
    }

}
