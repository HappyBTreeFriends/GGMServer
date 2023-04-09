using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// [size(2)][packetId(2)][...][size(2)][packetId(2)][...]
namespace ServerCore
{   
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;
        //sealed : 상속받은 다른 클래스가 OnRecv를 오버라이드 못하도록 봉인
        public override sealed int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {   
                if (buffer.Count < HeaderSize)  // 최소한 헤더(사이즈)는 받을 수 있는지 확인 
                    break;
                // 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)    // 부분적 도착
                    break;
                
                OnPacketRecv(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));  // 여기까지 왔으면 패킷 조립 가능

                processLen += dataSize;

                buffer = new ArraySegment<byte>(
                    buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }
            return processLen;
        }
        // 상속받은 클래스들은 OnPacketRecv()로 OnRecv를 우회하여 사용
        public abstract void OnPacketRecv(ArraySegment<byte>buffer);

    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0; // 인터락 플래그 변수

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        List<ArraySegment<byte>> _pendinglist = new List<ArraySegment<byte>>();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        object _lock = new object();    

        public void Init(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted); // (2-2) 낚시대 들기
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            //RegisterSend();

            RegisterRecv(); // (1) 낚시대 던지기
        }
        #region 데이터 수신
        // 1. 연결대기
        void RegisterRecv()
        {
            _recvBuffer.Clean(); // 커서 뒤로 이동 방지
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnRecvCompleted(null, _recvArgs);    // (2-1) 낚시대 들어올리기(데이터수신 발생)
        }
        // 2. 데이터수신
        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                //TODO
                 try
                {
                    // Write 커서 이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if(processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }
                    // Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }
                    
                    RegisterRecv(); // (3) 낚시대 다시 던지기(이벤트 재호출)
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
        #endregion
        // 송신파트
        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);    // 먼저, 보낼 데이터 큐에 넣고
                if (_pendinglist.Count == 0 )           // 대기 여부 판단해, 대기없으면
                    RegisterSend();             // send 예약
            }

        }

        void RegisterSend()
        {
            while(_sendQueue.Count > 0) 
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();          // 보낼 데이터 큐에서 꺼냄
                _pendinglist.Add(buff);  // 큐에서 꺼낸 buff값을 이벤트와 연결
            }
            _sendArgs.BufferList = _pendinglist;

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)   // 바로 보낼 데이터 있을 때, 없으면 이벤트 발생으로 OnSend 구동
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object send, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    //TODO
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendinglist.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                            RegisterSend();

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
            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}
