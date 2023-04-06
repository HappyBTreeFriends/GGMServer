// 커넥터
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Text;
using ServerCore;

namespace Server
{
 class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected bytes: {endPoint}");

            // 보낸다. (받기의 역순)
            byte[] sendBuff = Encoding.UTF8.GetBytes("Welcom To the Jungle!");
            Send(sendBuff);             // 웰컴투정글 송신

            Thread.Sleep(1000);                 // 1초 대기
            Disconnect();               // 세션 연결 종료

        }
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected bytes: {endPoint}");
        }
        public override void OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[ FROM CLIENT ] {recvData}");
        }
        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
    class Program
    {
        static Listener _listener = new Listener();

        static void Main(string[] args)
        {
            // DNS
            IPHostEntry iphost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = iphost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // IP주소, 포트번호 입력

            _listener.init(endPoint, () => { return new GameSession(); }  );
            Console.WriteLine("Listening...(영업중이야)");

            while (true)
            {
                //프로그램 종료 막기 위해 while
            }
        }

        
    }
}
