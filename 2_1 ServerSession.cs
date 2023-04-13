using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ServerCore;

namespace DummyClient
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
        PlayerInSfoOk = 2,
    }

    class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected bytes: {endPoint}");
            
            PlayerInfoReq packet = new PlayerInfoReq(){
                size = 4, packetId = (ushort)PacketID.PlayerInfoReq, playerId = 1001 };
            
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            ushort count = 0;
            bool success = true;

            // 혹시, 복사 실패할 경우 false 반환하여 예외처리, 주로 데이터 사이즈 문제로 false 반환.
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), packet.size);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), packet.packetId);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), packet.playerId);
            count += 8;

            ArraySegment<byte> sendbuff = SendBufferHelper.Close(count);
            if (success)
                Send(sendbuff);             // 송신
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
}
