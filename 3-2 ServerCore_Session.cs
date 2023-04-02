using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    class Session
    {
        Socket _socket;
        int _disconnected = 0; // 인터락 플래그 변수

        Queue<byte[]> sendQueue = new Queue<byte[]>();
        bool _pending = false;
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        object _lock = new object();

        public void Init(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted); // (2-2) 낚시대 들기
            recvArgs.SetBuffer(new byte[1024], 0, 1024);

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            //RegisterSend();

            RegisterRecv(recvArgs); // (1) 낚시대 던지기
        }
        #region 데이터 수신
        // 1. 연결대기
        void RegisterRecv(SocketAsyncEventArgs args)
        {
            bool pending = _socket.ReceiveAsync(args);
            if (pending == false)
                OnRecvCompleted(null, args);    // (2-1) 낚시대 들어올리기(데이터수신 발생)
        }
        // 2. 데이터수신
        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)  
        {           
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                //TODO
                try
                {
                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    Console.WriteLine($"[ FROM CLIENT ] {recvData}");

                    RegisterRecv(args); // (3) 낚시대 다시 던지기(이벤트 재호출)
                }
                catch(Exception e)
                {
                    Console.WriteLine($"OnReceiveCompleted Failed! {e}");
                }

            }
            else
            {
                Disconnect();
            }
        }
        #endregion
        // 송신파트
        public void Send(byte[] sendBuff)
        {
            lock(_lock)
            {
                sendQueue.Enqueue(sendBuff);    // 먼저, 보낼 데이터 큐에 넣고
                if (_pending == false)           // 대기 여부 판단해, 대기없으면
                    RegisterSend();             // send 예약
            }

        }

        void RegisterSend()
        {
            _pending = true;
            byte[] buff = sendQueue.Dequeue();          // 보낼 데이터 큐에서 꺼냄
            _sendArgs.SetBuffer(buff, 0, buff.Length);  // 큐에서 꺼낸 buff값을 이벤트와 연결

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)   // 바로 보낼 데이터 있을 때, 없으면 이벤트 발생으로 OnSend 구동
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object send, SocketAsyncEventArgs args)
        {
            lock(_lock) {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    //TODO
                    try
                    {
                        if(sendQueue.Count > 0)
                            RegisterSend();
                        else 
                            _pending = false; 
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnReceiveCompleted Failed! {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }



        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) // 연결종료 발생시 플래그변수 1로 변경
                return;                                         // 만약 이미 1로 변경되어있다면, return 통해 아무동작하지않음
            //쫓아낸다.
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}

