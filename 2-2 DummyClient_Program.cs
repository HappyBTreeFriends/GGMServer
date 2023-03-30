using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // DNS
            IPHostEntry iphost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = iphost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // IP주소, 포트번호 입력

            while (true) 
            { 
                // 휴대폰 설정
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // 문지기에게 입장 문의
                    socket.Connect(endPoint);   // 상대방 주소 입력,
                    Console.WriteLine($"Connected To {socket.RemoteEndPoint.ToString()}");  // 연결시, 서버 정보 출력

                    // 보낸다
                    byte[] sentBuff = Encoding.UTF8.GetBytes("Hello World!");
                    int sendBytes = socket.Send(sentBuff);

                    // 받는다
                    byte[] recvBuff = new byte[1024];
                    int recvBytes = socket.Receive(recvBuff);
                    string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                    Console.WriteLine($"FROM SERVER {recvData}");

                    //나간다.
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
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
