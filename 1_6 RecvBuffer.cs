using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    class RecvBuffer
    {
        // [r][ ][w][ ][ ][ ][ ][ ][ ][ ]
        ArraySegment<byte> _buffer; // Segment로 만들 경우, 대용량 바이트 대응 가능
        int _readPos;   // 현재 읽는 위치
        int _writePos;  // 현재 쓰는 위치

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        //버퍼 사용 범위
        public int DataSize { get { return _writePos - _readPos; } }
        //버퍼 남은 공간
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }
        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }
        //중간 중간 버퍼 위치 정리
        public void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0)  // rw가 같은 위치일때, 모든 데이터 처리한 상태
            {
                //남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
                _readPos = _writePos = 0;
            }
            else
            {
                //남은 데이터가 있으면, 시작 위치로 복사
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos= dataSize;
            }
        }
        // 데이터를 성공적으로 Read 처리 했을 때
        public bool OnRead(int numOfBytes)
        {
            if(numOfBytes > DataSize)
                return false;
            _readPos += numOfBytes;
            return true;
        }
        // 데이터를 Recv해서 받았을 때 = Write해야할 때
        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;
            _writePos += numOfBytes;
            return true;
        }
    }
}


// [rw][ ][ ][ ][ ][ ][ ][ ][ ][ ]

// [r][ ][ ][ ][ ][w][ ][ ][ ][ ]

// [r][ ][ ][ ][ ][ ][ ][ ][w][ ]

// [rw][ ][ ][ ][ ][ ][ ][ ][ ][ ]
