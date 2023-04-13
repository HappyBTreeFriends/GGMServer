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
    public abstract class Packet
    {
        public ushort size;
        public ushort packetId;

        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> s);
    }

    class PlayerInfoReq : Packet
    {
        public long playerId;
        public string name;

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;

            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);  // 파싱

            //ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += sizeof(ushort);
            //ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += sizeof(ushort);
            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));  // PlayerID 파싱
            count += sizeof(long); // 데이터 크기 누적
             
            // string 파싱
            ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            Encoding.Unicode.GetString(s.Slice(count, nameLen));

        }

        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacketID.PlayerInfoReq;
        }

        public override ArraySegment<byte> Write()
        {
            // 직렬화
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);
            ushort count = 0;
            bool success = true;

            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            // 혹시, 복사 실패할 경우 false 반환하여 예외처리, 주로 데이터 사이즈 문제로 false 반환.
            //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), this.size);
            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
            count += sizeof(ushort);    
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
            count += sizeof(long);
            

            // string 송신 절차
            //ushort nameLen = (ushort)Encoding.Unicode.GetByteCount(this.name);   // UTF16으로 name의 길이 반환
            //success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);   // 길이 입력
            //count += sizeof(ushort);
            //Encoding.Unicode.GetBytes(this.name);   // 원본메시지 내용 byte로 변환
            //Array.Copy(Encoding.Unicode.GetBytes(this.name), 0, segment.Array, count, nameLen); // segment로 string 길이 만큼 복사
            //count += nameLen;

            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(
                this.name, 0, this.name.Length, segment.Array, segment.Offset + count);   // UTF16으로 name의 길이 반환
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
            count += sizeof(ushort);
            count += nameLen;

            success &= BitConverter.TryWriteBytes(s, count);

            if (success == false)   // 예외처리
                return null;

            return SendBufferHelper.Close(count);
        }
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
            
            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name = "ABCD" };
            ArraySegment<byte> s = packet.Write();
            if (s != null)  // Write() 내부에서 null로 예외처리 체크
                Send(s);             // 송신
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
