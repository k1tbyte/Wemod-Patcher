using System;
using System.Text;

namespace AsarSharp.PickleTools
{
   public class PickleIterator
    {
        private readonly byte[] _payload;
        private readonly int _payloadOffset;
        private int _readIndex;
        private readonly int _endIndex;

        public PickleIterator(Pickle pickle)
        {
            _payload = pickle.GetHeader();
            _payloadOffset = pickle.GetHeaderSize();
            _readIndex = 0;
            _endIndex = pickle.GetPayloadSize();
        }

        public bool ReadBool()
        {
            return ReadInt() != 0;
        }

        public int ReadInt()
        {
            return ReadBytes(Pickle.SIZE_INT32, BitConverter.ToInt32);
        }

        public uint ReadUInt32()
        {
            return ReadBytes(Pickle.SIZE_UINT32, BitConverter.ToUInt32);
        }

        public long ReadInt64()
        {
            return ReadBytes(Pickle.SIZE_INT64, BitConverter.ToInt64);
        }

        public ulong ReadUInt64()
        {
            return ReadBytes(Pickle.SIZE_UINT64, BitConverter.ToUInt64);
        }

        public float ReadFloat()
        {
            return ReadBytes(Pickle.SIZE_FLOAT, BitConverter.ToSingle);
        }

        public double ReadDouble()
        {
            return ReadBytes(Pickle.SIZE_DOUBLE, BitConverter.ToDouble);
        }

        public string ReadString()
        {
            int length = ReadInt();
            return Encoding.UTF8.GetString(ReadBytes(length));
        }

        private T ReadBytes<T>(int length, Func<byte[], int, T> converter)
        {
            int readPayloadOffset = GetReadPayloadOffsetAndAdvance(length);
            return converter(_payload, readPayloadOffset);
        }

        private byte[] ReadBytes(int length)
        {
            int readPayloadOffset = GetReadPayloadOffsetAndAdvance(length);
            byte[] result = new byte[length];
            Array.Copy(_payload, readPayloadOffset, result, 0, length);
            return result;
        }

        private int GetReadPayloadOffsetAndAdvance(int length)
        {
            if (length > _endIndex - _readIndex)
            {
                _readIndex = _endIndex;
                throw new InvalidOperationException($"Failed to read data with length of {length}");
            }
            int readPayloadOffset = _payloadOffset + _readIndex;
            Advance(length);
            return readPayloadOffset;
        }

        private void Advance(int size)
        {
            int alignedSize = Pickle.AlignInt(size, Pickle.SIZE_UINT32);
            if (_endIndex - _readIndex < alignedSize)
            {
                _readIndex = _endIndex;
            }
            else
            {
                _readIndex += alignedSize;
            }
        }
    }
}