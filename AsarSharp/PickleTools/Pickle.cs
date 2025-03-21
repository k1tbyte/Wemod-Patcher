using System;
using System.Text;

namespace AsarSharp.PickleTools
{
    public class Pickle
    {
        public const int SIZE_INT32 = 4;
        public const int SIZE_UINT32 = 4;
        public const int SIZE_INT64 = 8;
        public const int SIZE_UINT64 = 8;
        public const int SIZE_FLOAT = 4;
        public const int SIZE_DOUBLE = 8;

        // Size of memory allocation unit for payload
        public const int PAYLOAD_UNIT = 64;

        // Maximum value for read-only
        public const long CAPACITY_READ_ONLY = 9007199254740992;

        private byte[] _header;
        private int _headerSize;
        private long _capacityAfterHeader;
        private int _writeOffset;

        private Pickle(byte[] buffer = null)
        {
            if (buffer != null)
            {
                _header = buffer;
                _headerSize = buffer.Length - GetPayloadSize();
                _capacityAfterHeader = CAPACITY_READ_ONLY;
                _writeOffset = 0;

                if (_headerSize > buffer.Length)
                {
                    _headerSize = 0;
                }

                if (_headerSize != AlignInt(_headerSize, SIZE_UINT32))
                {
                    _headerSize = 0;
                }

                if (_headerSize == 0)
                {
                    _header = new byte[0];
                }
            }
            else
            {
                _header = new byte[0];
                _headerSize = SIZE_UINT32;
                _capacityAfterHeader = 0;
                _writeOffset = 0;
                Resize(PAYLOAD_UNIT);
                SetPayloadSize(0);
            }
        }
        
        public static Pickle CreateEmpty()
        {
            return new Pickle();
        }
        
        public static Pickle CreateFromBuffer(byte[] buffer)
        {
            return new Pickle(buffer);
        }

        public byte[] GetHeader()
        {
            return _header;
        }

        public int GetHeaderSize()
        {
            return _headerSize;
        }
        
        public PickleIterator CreateIterator()
        {
            return new PickleIterator(this);
        }

        /// <summary>
        /// Converts Pickle to a byte array
        /// </summary>
        public byte[] ToBuffer()
        {
            int resultSize = _headerSize + GetPayloadSize();
            byte[] result = new byte[resultSize];
            Array.Copy(_header, 0, result, 0, resultSize);
            return result;
        }


        public bool WriteBool(bool value)
        {
            return WriteInt(value ? 1 : 0);
        }
        
        public bool WriteInt(int value)
        {
            EnsureCapacity(SIZE_INT32);

            var dataLength = AlignInt(SIZE_INT32, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            WriteInt32LE(value, _headerSize + _writeOffset);

            var endOffset = _headerSize + _writeOffset + SIZE_INT32;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_INT32; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }


        public bool WriteUInt32(uint value)
        {
            EnsureCapacity(SIZE_UINT32);

            var dataLength = AlignInt(SIZE_UINT32, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            WriteUInt32LE(value, _headerSize + _writeOffset);

            var endOffset = _headerSize + _writeOffset + SIZE_UINT32;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_UINT32; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }
        
        public bool WriteInt64(long value)
        {
            EnsureCapacity(SIZE_INT64);

            var dataLength = AlignInt(SIZE_INT64, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            WriteInt64LE(value, _headerSize + _writeOffset);

            var endOffset = _headerSize + _writeOffset + SIZE_INT64;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_INT64; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }


        public bool WriteUInt64(ulong value)
        {
            EnsureCapacity(SIZE_UINT64);

            var dataLength = AlignInt(SIZE_UINT64, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            WriteUInt64LE(value, _headerSize + _writeOffset);

            var endOffset = _headerSize + _writeOffset + SIZE_UINT64;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_UINT64; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }
        
        public bool WriteFloat(float value)
        {
            EnsureCapacity(SIZE_FLOAT);

            var dataLength = AlignInt(SIZE_FLOAT, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            Array.Copy(bytes, 0, _header, _headerSize + _writeOffset, SIZE_FLOAT);

            var endOffset = _headerSize + _writeOffset + SIZE_FLOAT;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_FLOAT; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }
        
        public bool WriteDouble(double value)
        {
            EnsureCapacity(SIZE_DOUBLE);

            var dataLength = AlignInt(SIZE_DOUBLE, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            Array.Copy(bytes, 0, _header, _headerSize + _writeOffset, SIZE_DOUBLE);

            var endOffset = _headerSize + _writeOffset + SIZE_DOUBLE;
            for (int i = endOffset; i < endOffset + dataLength - SIZE_DOUBLE; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }
        
        public bool WriteString(string value)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(value);
            int length = strBytes.Length;

            if (!WriteInt(length))
            {
                return false;
            }

            var dataLength = AlignInt(length, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }

            Array.Copy(strBytes, 0, _header, _headerSize + _writeOffset, length);

            var endOffset = _headerSize + _writeOffset + length;
            for (int i = endOffset; i < endOffset + dataLength - length; i++)
            {
                _header[i] = 0;
            }

            SetPayloadSize(newSize);
            _writeOffset = newSize;
            return true;
        }
        
        public void SetPayloadSize(int payloadSize)
        {
            WriteUInt32LE((uint)payloadSize, 0);
        }
        
        public int GetPayloadSize()
        {
            return (int)ReadUInt32LE(0);
        }
        
        private void Resize(int newCapacity)
        {
            newCapacity = AlignInt(newCapacity, PAYLOAD_UNIT);
            byte[] newHeader = new byte[_header.Length + newCapacity];
            Array.Copy(_header, 0, newHeader, 0, _header.Length);
            _header = newHeader;
            _capacityAfterHeader = newCapacity;
        }
        
        public static int AlignInt(int i, int alignment)
        {
            return i + ((alignment - (i % alignment)) % alignment);
        }
        
        private void EnsureCapacity(int additionalSize)
        {
            var dataLength = AlignInt(additionalSize, SIZE_UINT32);
            var newSize = _writeOffset + dataLength;

            if (newSize > _capacityAfterHeader)
            {
                Resize(Math.Max((int)_capacityAfterHeader * 2, newSize));
            }
        }

        #region Auxiliary methods for reading/writing values in Little Endian

        private uint ReadUInt32LE(int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt32(_header, offset);
            }
            else
            {
                return (uint)(_header[offset] |
                              (_header[offset + 1] << 8) |
                              (_header[offset + 2] << 16) |
                              (_header[offset + 3] << 24));
            }
        }

        private void WriteInt32LE(int value, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, _header, offset, 4);
            }
            else
            {
                _header[offset] = (byte)value;
                _header[offset + 1] = (byte)(value >> 8);
                _header[offset + 2] = (byte)(value >> 16);
                _header[offset + 3] = (byte)(value >> 24);
            }
        }

        private void WriteUInt32LE(uint value, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, _header, offset, 4);
            }
            else
            {
                _header[offset] = (byte)value;
                _header[offset + 1] = (byte)(value >> 8);
                _header[offset + 2] = (byte)(value >> 16);
                _header[offset + 3] = (byte)(value >> 24);
            }
        }

        private void WriteInt64LE(long value, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, _header, offset, 8);
            }
            else
            {
                _header[offset] = (byte)value;
                _header[offset + 1] = (byte)(value >> 8);
                _header[offset + 2] = (byte)(value >> 16);
                _header[offset + 3] = (byte)(value >> 24);
                _header[offset + 4] = (byte)(value >> 32);
                _header[offset + 5] = (byte)(value >> 40);
                _header[offset + 6] = (byte)(value >> 48);
                _header[offset + 7] = (byte)(value >> 56);
            }
        }
        
        private void WriteUInt64LE(ulong value, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, _header, offset, 8);
            }
            else
            {
                _header[offset] = (byte)value;
                _header[offset + 1] = (byte)(value >> 8);
                _header[offset + 2] = (byte)(value >> 16);
                _header[offset + 3] = (byte)(value >> 24);
                _header[offset + 4] = (byte)(value >> 32);
                _header[offset + 5] = (byte)(value >> 40);
                _header[offset + 6] = (byte)(value >> 48);
                _header[offset + 7] = (byte)(value >> 56);
            }
        }

        
        #endregion
    }
}