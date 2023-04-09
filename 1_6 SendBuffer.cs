using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(
            () => { return null; } );
        public static int chunkSize { get; set; } = 4096; // 사이즈 변경 희망시

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(chunkSize);
            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(chunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }
        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }



    public class SendBuffer
    {
        // [ ][ ][ ][ ][ ][u][ ][ ][ ][ ]
        byte[] _buffer; // 
        int _usedSize = 0; // 사용한 데이터 크기 커서
        public int FreeSize {  get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }
        public ArraySegment<byte> Open(int reserveSize) // 얼마큼 버퍼에 전송할건지 최대치 설정
        {
            if (reserveSize > FreeSize) // 여유공간보다 더 요청할 경우
                return null;

            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize)   // 버퍼 전송한 뒤, 실제 사용한 양 보고
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;  // u커서 이동
            return segment;
        }
        // SendBuffer는 Clean()이 없다.
        // 일회용으로만 사용, 재사용할 경우 멀티스레드 환경에서 SendBuffer를 누군가 참조할 경우 오류 유발.
    }
}
