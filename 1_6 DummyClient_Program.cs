using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    class Packet
    {
        public ushort size;
        public ushort packetId;
    }

    class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected bytes: {endPoint}");

            Packet packet = new Packet() { size = 4, packetId = 7 };
            for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> opensegment = SendBufferHelper.Open(4096);
                byte[] buffer = BitConverter.GetBytes(packet.size);
                byte[] buffer2 = BitConverter.GetBytes(packet.packetId);

                Array.Copy(buffer, 0, opensegment.Array, opensegment.Offset, buffer.Length);
                Array.Copy(buffer2, 0, opensegment.Array, opensegment.Offset + buffer.Length, buffer2.Length);
                ArraySegment<byte> sendbuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

                // 보낸다. (받기의 역순)
                //byte[] sendbuff = encoding.utf8.getbytes("welcom to the jungle!");
                Send(sendbuff);             // 웰컴투정글 송신
            }

        }
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected bytes: {endPoint}");
        }
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[ FROM Server ] {recvData}");
            return buffer.Count;
        }
        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // DNS
            IPHostEntry iphost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = iphost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // IP주소, 포트번호 입력

            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return new GameSession(); });
            while (true)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(100);
            }

        }
    }
}
