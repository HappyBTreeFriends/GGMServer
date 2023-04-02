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
                Session session = new Session();    // 데이터 송수신 세션 개설
                session.Init(clientSocket);         // 세션 동작 초기화

                // 보낸다. (받기의 역순)
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcom To the Jungle!");
                session.Send(sendBuff);             // 웰컴투정글 송신

                Thread.Sleep(1000);                 // 1초 대기

                session.Disconnect();               // 세션 연결 종료


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

