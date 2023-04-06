using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ServerCore
{
    internal class Program
    {
        static Listener _listener = new Listener();

        static void Main(string[] args)
        {
            // DNS
            IPHostEntry iphost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = iphost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // IP주소, 포트번호 입력

            _listener.init(endPoint, OnAcceptHandler);
            Console.WriteLine("Listening...(영업중이야)");

            while (true)
            {
                //프로그램 종료 막기 위해 while
            }
        }

        static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                // 받는다.
                byte[] recvBuff = new byte[1024];
                int recvBytes = clientSocket.Receive(recvBuff); // 1. 데이터를 recvBuff에 받고, 2. 받은 데이터량을 계산한다.
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"[FROM CLIENT] {recvData}");

                // 보낸다. (받기의 역순)
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcom To the Jungle!");
                clientSocket.Send(sendBuff);

                // 빠이빠이, 쫓아내기
                clientSocket.Shutdown(SocketShutdown.Both); // 종료 예고
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

