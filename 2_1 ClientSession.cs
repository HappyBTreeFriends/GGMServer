using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using ServerCore;


namespace SServer
{
    class Packet
    {
        public ushort size;
        public ushort packetId;
    }

    class PlayerInfoReq : Packet
    {
        public long playerId;
    }

    class PlayerInfoOk : Packet
    {
        public int hp;
        public int attack;
    }

    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,
    }

    class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected bytes: {endPoint}");

            //Packet packet = new Packet() { size = 100, packetId = 100 };

            //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            //byte[] buffer = BitConverter.GetBytes(packet.size);
            //byte[] buffer2 = BitConverter.GetBytes(packet.packetId);

            //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
            //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

            //// 보낸다. (받기의 역순)
            ////byte[] sendBuff = Encoding.UTF8.GetBytes("Welcom To the Jungle!");
            //Send(sendBuff);             // 웰컴투정글 송신

            Thread.Sleep(5000);                 // 1초 대기
            Disconnect();               // 세션 연결 종료
        }
        // 유효한 패킷을 처리
        public override void OnPacketRecv(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketID)id)
            {
                case PacketID.PlayerInfoReq:    //플레이어 정보 요청 패킷일때
                    {
                        long playerId = BitConverter.ToInt64(buffer.Array, buffer.Offset + count);  // 파싱
                        count += 8; // 데이터 크기 누적
                        Console.WriteLine($"PlayerInfoReq: {playerId}");    // 화면 출력

                    }
                    break;
            }
   
            Console.WriteLine($"RecvPacketId: {id}, Size {size}");
        }
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected bytes: {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

    }
}
