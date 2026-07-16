using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using MessagePack;
using MessagePack.Resolvers;

namespace UDPGame
{
    public class UDPGameClient
    {
        UdpClient udpClient;
        IPEndPoint endPoint;
        Action<byte[]> OnDataRecieved;
   
        public UDPGameClient(Action<byte[]> onDataRecieved)
        {
            udpClient = new UdpClient();
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            int serverPort = 1234;
            endPoint = new IPEndPoint(serverIP, serverPort);
            udpClient.Connect(endPoint);
            Thread recieveTrhread = new Thread(() => RecieveDataFromServer());
            recieveTrhread.IsBackground = true;
            recieveTrhread.Start();
            this.OnDataRecieved = onDataRecieved;

        }      
        ///only reason for async is to support the fake latency
        public async void SendDataToServer(NetworkMessage message)
        {
            await Task.Delay(Game1.LATENCY);
            byte[] messageBytes = new byte[1024];
            byte messageTypeByte = message.GetMessageTypeAsByte;
            switch (message.MessageType)
            {
                //We dont wont to send snapshots, only recive :)
                case MessageType.MovementUpdate:
                    messageBytes = MessagePackSerializer.Serialize((MovementUpdate)message);
                    break;
                default:
                    break;
            }
            byte[] combinedBytes = new byte[1 + messageBytes.Length];
            combinedBytes[0] = messageTypeByte;
            Buffer.BlockCopy(messageBytes, 0, combinedBytes, 1, messageBytes.Length);
            udpClient.Send(combinedBytes);
        }
        void RecieveDataFromServer()
        {
            while (true)
            {
                byte[] serverResponse = udpClient.Receive(ref endPoint);
                OnDataRecieved.Invoke(serverResponse);
            }
        }
    }
}
