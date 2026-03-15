#define BLANK_BYTE_ARRAY_ENDIAN_BIG
//#define SAFETY_CHECK

using System.Runtime.CompilerServices;

namespace SIE.IO
{
    using System;
    using System.IO;

    public class ByteArray
    {
        //默认容量
        protected const int DEFAULT_CAPACITY = 32;

        //默认增长因子
        protected const float DEFAULT_GROWTH_FACTOR = 0.75f;

        //字节数组
        protected byte[] _bytes = null;

        //当前位置
        protected int _pos = 0;

        //长度
        protected int _length = 0;

        //byteArr字节数组的长度
        protected int _capacity = 0;

        //增长因子(较原 byte[] 增长的长度比,比如: 0.75F 表示每次增长的长度为原长度乘以 0.75F )
        protected float _growthFactor = DEFAULT_GROWTH_FACTOR;

        /**
		 * 字节数组
		 */
        public ByteArray() {
            _bytes = new byte[DEFAULT_CAPACITY];
            _capacity = DEFAULT_CAPACITY;
        }

        /**
		 * 字节数组
		 *
		 * @param capacity 初始长度(字节数)
		 */
        public ByteArray(int capacity) {
            _bytes = new byte[capacity];
            _capacity = capacity;
        }

        /**
		 * 字节数组(以 byte[] 构建,注意:此处为引用传递)
		 *
		 * @param bytes 初始数据
		 */
        public ByteArray(byte[] bytes) {
            _bytes = bytes;
            _length = _bytes.Length;
            _capacity = _length;
        }

        /**
		 * 扩展字节数组的长度,扩展的长度为指定的增长因子
		 */
        public ByteArray Grow() {
            var newL = _capacity + (int)(_capacity * _growthFactor);

            Array.Resize(ref _bytes, newL);
            _capacity = newL;

            /*byte[] copy = new byte[newL];
            Array.Copy(_bytes, copy, _capacity);

            _bytes = copy;
            _capacity = newL;*/

            return this;
        }

        /**
		 * 扩展字节数组的长度
		 *
		 * @param growLen
		 */
        public ByteArray Grow(int growLen) {
            var newL = _capacity + Math.Max(growLen, (int)(_capacity * _growthFactor));
            //byte[] copy = new byte[newL];
            //Array.Copy(_bytes, copy, _capacity);

            //_bytes = copy;

            Array.Resize(ref _bytes, newL);
            _capacity = newL;

            return this;
        }

        public ByteArray HintSize(int size) {
            if (_bytes.Length - _pos < size) {
                Grow(size - (_bytes.Length - _pos));
            }
            return this;
        }

        /**
		 * 清除所有数据,功能等于 setLength(0)
		 */
        public ByteArray Clear() {
            Length = 0;

            return this;
        }

        /// <summary>获取字节数组</summary>
        /// <param name="raw">如果为真，则返回原始数据，否则拷贝有效数据到新的数组</param>
        /// <returns>字节数组</returns>
        public byte[] Bytes(bool raw = true) {
            if (raw) return _bytes;

            var bytes = CloneAllBytes();
            //Array.Copy(_bytes, bytes, _length);

            return bytes;
        }

        public byte[] CloneAllBytes(bool clear = false) {
            if (_length <= 0) {
                return null;
            }

            var bytes = new byte[_length];
            unsafe {
                fixed (byte* d = bytes, s = _bytes) {
                    Unsafe.CopyBlock(d, s, (uint)_length);
                }
            }

            if (clear) {
                Length = 0;
            }

            return bytes;
        }

        public string AsUTF8String(bool clear = false) {
            var t = System.Text.Encoding.UTF8.GetString(_bytes, 0, _length);

            if (clear) {
                Length = 0;
            }

            return t;
        }

        public int Capacity => _capacity;

        public float GrowthFactor {
            set {
                if (value < 0.0f) {
                    throw new Exception("Growth factor cannot be negative");
                }

                _growthFactor = value;
            }
            get => _growthFactor;
        }

        public int Length {
            set {
                if (value < 0) {
                    throw new Exception("Length cannot be negative");
                }

                _length = value;
                if (value > _capacity) {
                    Grow(_capacity - value);
                } else {
                    if (_pos > _length) {
                        _pos = _length;
                    }
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public int Position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if (value < 0 || value > _length) {
                    throw new Exception("error in Position => value out of range");
                }

                _pos = value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pos;
        }

        public int ByteAvailable {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length - _pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Boolean ReadBoolean() => ReadUInt8() != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteArray WriteBoolean(Boolean value) => WriteInt8(value ? 1 : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadInt8() {
#if SAFETY_CHECK
			if(_pos >= _length){
				throw new Exception("error in ReadInt8 => read data overflow");
			}
#endif
            return (sbyte)_bytes[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8() {
#if SAFETY_CHECK
			if(_pos >= _length){
				throw new Exception("error in ReadUInt8 => read data overflow");
			}
#endif
            return _bytes[_pos++];
        }

        public ByteArray WriteInt8(int value) {
            if (_pos + 1 > _capacity) {
                Grow(1);
            }
            _bytes[_pos++] = (byte)value;

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public Int16 ReadInt16() {
#if SAFETY_CHECK
			if (_pos + 2 > _length) {
				throw new Exception("error in ReadInt16 => read data overflow");
			}
#endif
            _pos += 2;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (Int16)(_bytes[_pos - 1] | ((_bytes[_pos - 2]) << 8));
#else
			return (Int16)(_bytes[_pos - 2] | ((_bytes[_pos - 1]) << 8));
#endif
        }

        public UInt16 ReadUInt16() {
#if SAFETY_CHECK
			if (_pos + 2 > _length) {
				throw new Exception("error in ReadUInt16 => read data overflow");
			}
#endif
            _pos += 2;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (UInt16)(_bytes[_pos - 1] | ((_bytes[_pos - 2]) << 8));
#else
			return (UInt16)(_bytes[_pos - 2] | ((_bytes[_pos - 1]) << 8));
#endif
        }

        public ByteArray WriteInt16(int value) {
            if (_pos + 2 > _capacity) {
                Grow(2);
            }
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)(value >> 8);
            _bytes[_pos++] = (byte)value;
#else
			_bytes[_pos++] = (byte)value;
			_bytes[_pos++] = (byte)(value >> 8);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public static uint ZigZagEncode(int v) {
            return (uint)((v << 1) ^ (v >> 31));
        }

        public static int ZigZagDecode(uint v) {
            return (int)((v >> 1) ^ -(v & 1));
        }

        /// <summary>
        /// 解决 uleb128 传入负数时，占用空间过大问题
        /// 但是对于 > 0的数 储存空间利用率还是没有 uleb128 高
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public ByteArray WriteZigZag(int v) {
            uint r_value = ZigZagEncode(v);
            return WriteULEB128((int)r_value);
        }

        public int ReadZigZag() {
            int v = ReadULEB128();
            return ZigZagDecode((uint)v);
        }

        public int ReadULEB128() {
            int v = 0;
            int slider = 0;
            while (true) {
#if SAFETY_CHECK
                if (_pos >= _length) {
                    throw new Exception("error in ReadULEB128 => read data overflow");
                }
#endif
                int temp = _bytes[_pos++];
                if (temp <= 127) {
                    temp <<= slider;
                    v += temp;
                    break;
                } else {
                    temp &= 127;
                    temp <<= slider;
                    v += temp;
                }
                slider += 7;
            }

            return v;
        }

        public long ReadULEB128_Int64()
        {
            long v = 0;
            int shift = 0;

            while (true)
            {
#if SAFETY_CHECK
        if (_pos >= _length)
            throw new Exception("Read data overflow");
#endif

                int temp = _bytes[_pos++];

                v |= ((long)(temp & 0x7F)) << shift;

                if ((temp & 0x80) == 0) break;

                shift += 7;
                if (shift >= 64) throw new FormatException("Invalid ULEB128 Int64");
            }

            return v;
        }

        /// <summary>
        /// 动态int长度写入
        /// 理论上只能写入 > 0 的整数
        /// 如果是负数会占用更大的储存空间
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ByteArray WriteULEB128(int value) {
            uint r_value = (uint)value;
            if (r_value <= 127) {
                if (_pos >= _capacity) {
                    Grow(1);
                }
            } else if (r_value <= 0x3fff) {
                if (_pos + 2 > _capacity) {
                    Grow(2);
                }
            } else if (r_value <= 0x1fffff) {
                if (_pos + 3 > _capacity) {
                    Grow(3);
                }
            } else {
                if (_pos + 5 > _capacity) {
                    Grow(5);
                }
            }

            while (true) {
                if (r_value > 127) {
                    _bytes[_pos++] = (byte)((r_value & 127) | 128);
                    r_value >>= 7;
                } else {
                    _bytes[_pos++] = (byte)r_value;
                    break;
                }
            }

            if (_length < _pos) {
                _length = _pos;
            }
            return this;
        }

        /// <summary>
        /// 动态 long 长度写入（ULEB128）
        /// 理论上只能写入 > 0 的整数
        /// 如果是负数会占用更大的储存空间
        /// </summary>
        public ByteArray WriteULEB128(long value)
        {
            ulong r_value = (ulong)value;

            if (r_value <= 0x7FUL) {
                if (_pos >= _capacity) {
                    Grow(1);
                }
            }
            else if (r_value <= 0x3FFFUL) {
                if (_pos + 2 > _capacity) {
                    Grow(2);
                }
            }
            else if (r_value <= 0x1FFFFFUL) {
                if (_pos + 3 > _capacity) {
                    Grow(3);
                }
            }
            else if (r_value <= 0xFFFFFFFUL) {
                if (_pos + 4 > _capacity) {
                    Grow(4);
                }
            }
            else if (r_value <= 0x7FFFFFFFFUL) {
                if (_pos + 5 > _capacity) {
                    Grow(5);
                }
            }
            else if (r_value <= 0x3FFFFFFFFFFUL) {
                if (_pos + 6 > _capacity) {
                    Grow(6);
                }
            }
            else if (r_value <= 0x1FFFFFFFFFFFFUL) {
                if (_pos + 7 > _capacity) {
                    Grow(7);
                }
            }
            else if (r_value <= 0xFFFFFFFFFFFFFFUL) {
                if (_pos + 8 > _capacity) {
                    Grow(8);
                }
            }
            else if (r_value <= 0x7FFFFFFFFFFFFFFFUL) {
                if (_pos + 9 > _capacity) {
                    Grow(9);
                }
            }
            else {
                if (_pos + 10 > _capacity) {
                    Grow(10);
                }
            }

            while (true) {
                if (r_value > 127) {
                    _bytes[_pos++] = (byte)((r_value & 127) | 128);
                    r_value >>= 7;
                }
                else {
                    _bytes[_pos++] = (byte)r_value;
                    break;
                }
            }

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public int ReadInt32() {
#if SAFETY_CHECK
			if (_pos + 4 > _length) {
				throw new Exception("error in ReadInt32 => read data overflow");
			}
#endif
            _pos += 4;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (_bytes[_pos - 1]) | ((_bytes[_pos - 2]) << 8) | ((_bytes[_pos - 3]) << 16) | ((_bytes[_pos - 4]) << 24);
#else
			return (_bytes[_pos - 4]) | ((_bytes[_pos - 3]) << 8) | ((_bytes[_pos - 2]) << 16) | ((_bytes[_pos - 1]) << 24);
#endif
        }

        public int ReadInt32(byte mask) {
#if SAFETY_CHECK
			if (_pos + 4 > _length) {
				throw new Exception("error in ReadInt32 => read data overflow");
			}
#endif
            _pos += 4;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (_bytes[_pos - 1] ^ mask) | ((_bytes[_pos - 2] ^ mask) << 8) | ((_bytes[_pos - 3] ^ mask) << 16) | ((_bytes[_pos - 4] ^ mask) << 24);
#else
			return (_bytes[_pos - 4] ^ mask) | ((_bytes[_pos - 3] ^ mask) << 8) | ((_bytes[_pos - 2] ^ mask) << 16) | ((_bytes[_pos - 1] ^ mask) << 24);
#endif
        }

        public UInt32 ReadUInt32() {
#if SAFETY_CHECK
			if (_pos + 4 > _length) {
				throw new Exception("error in ReadUInt32 => read data overflow");
			}
#endif
            _pos += 4;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (UInt32)((_bytes[_pos - 1]) | ((_bytes[_pos - 2]) << 8) | ((_bytes[_pos - 3]) << 16) | ((_bytes[_pos - 4]) << 24));
#else
			return (UInt32)((_bytes[_pos - 4]) | ((_bytes[_pos - 3]) << 8) | ((_bytes[_pos - 2]) << 16) | ((_bytes[_pos - 1]) << 24));
#endif
        }

        public UInt32 ReadUInt32(byte mask) {
#if SAFETY_CHECK
			if (_pos + 4 > _length) {
				throw new Exception("error in ReadUInt32 => read data overflow");
			}
#endif
            _pos += 4;

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return (UInt32)((_bytes[_pos - 1] ^ mask) | ((_bytes[_pos - 2] ^ mask) << 8) | ((_bytes[_pos - 3] ^ mask) << 16) | ((_bytes[_pos - 4] ^ mask) << 24));
#else
			return (UInt32)((_bytes[_pos - 4] ^ mask) | ((_bytes[_pos - 3] ^ mask) << 8) | ((_bytes[_pos - 2] ^ mask) << 16) | ((_bytes[_pos - 1] ^ mask) << 24));
#endif
        }

        public ByteArray WriteInt32(int value) {
            if (_pos + 4 > _capacity) {
                Grow(4);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)(value >> 24);
            _bytes[_pos++] = (byte)(value >> 16);
            _bytes[_pos++] = (byte)(value >> 8);
            _bytes[_pos++] = (byte)value;
#else
			_bytes[_pos++] = (byte)value;
			_bytes[_pos++] = (byte)(value >> 8);
			_bytes[_pos++] = (byte)(value >> 16);
			_bytes[_pos++] = (byte)(value >> 24);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray WriteInt32(int value, int mask) {
            if (_pos + 4 > _capacity) {
                Grow(4);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)((value >> 24) ^ mask);
            _bytes[_pos++] = (byte)((value >> 16) ^ mask);
            _bytes[_pos++] = (byte)((value >> 8) ^ mask);
            _bytes[_pos++] = (byte)(value ^ mask);
#else
			_bytes[_pos++] = (byte)(value ^ mask);
			_bytes[_pos++] = (byte)((value >> 8) ^ mask);
			_bytes[_pos++] = (byte)((value >> 16) ^ mask);
			_bytes[_pos++] = (byte)((value >> 24) ^ mask);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray WriteUInt32(UInt32 value) {
            if (_pos + 4 > _capacity) {
                Grow(4);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)(value >> 24);
            _bytes[_pos++] = (byte)(value >> 16);
            _bytes[_pos++] = (byte)(value >> 8);
            _bytes[_pos++] = (byte)value;
#else
			_bytes[_pos++] = (byte)value;
			_bytes[_pos++] = (byte)(value >> 8);
			_bytes[_pos++] = (byte)(value >> 16);
			_bytes[_pos++] = (byte)(value >> 24);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray WriteUInt32(UInt32 value, byte mask) {
            if (_pos + 4 > _capacity) {
                Grow(4);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)((value >> 24) ^ mask);
            _bytes[_pos++] = (byte)((value >> 16) ^ mask);
            _bytes[_pos++] = (byte)((value >> 8) ^ mask);
            _bytes[_pos++] = (byte)(value ^ mask);
#else
			_bytes[_pos++] = (byte)(value ^ mask);
			_bytes[_pos++] = (byte)((value >> 8) ^ mask);
			_bytes[_pos++] = (byte)((value >> 16) ^ mask);
			_bytes[_pos++] = (byte)((value >> 24) ^ mask);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public Int64 ReadInt64() {
#if SAFETY_CHECK
			if (_pos + 8 > _length) {
				throw new Exception("error in ReadInt64 => read data overflow");
			}
#endif
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return ((Int64)(ReadInt32()) << 32) | ReadUInt32();
#else
			return ReadUInt32() | ((Int64)(ReadInt32()) << 32);
#endif
        }

        public Int64 ReadInt64(byte mask) {
#if SAFETY_CHECK
			if (_pos + 8 > _length) {
				throw new Exception("error in ReadInt64 => read data overflow");
			}
#endif
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return ((Int64)(ReadInt32(mask)) << 32) | ReadUInt32(mask);
#else
			return ReadUInt32(mask) | ((Int64)(ReadInt32(mask)) << 32);
#endif
        }

        public UInt64 ReadUInt64() {
#if SAFETY_CHECK
			if (_pos + 8 > _length) {
				throw new Exception("error in ReadUInt64 => read data overflow");
			}
#endif
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            return ((UInt64)(ReadUInt32()) << 32) | ReadUInt32();
#else
			return ReadUInt32() | ((UInt64)(ReadUInt32()) << 32);
#endif
        }

        public ByteArray WriteInt64(Int64 value) {
            if (_pos + 8 > _capacity) {
                Grow(8);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)(value >> 56);
            _bytes[_pos++] = (byte)(value >> 48);
            _bytes[_pos++] = (byte)(value >> 40);
            _bytes[_pos++] = (byte)(value >> 32);
            _bytes[_pos++] = (byte)(value >> 24);
            _bytes[_pos++] = (byte)(value >> 16);
            _bytes[_pos++] = (byte)(value >> 8);
            _bytes[_pos++] = (byte)value;
#else
			_bytes[_pos++] = (byte)value;
			_bytes[_pos++] = (byte)(value >> 8);
			_bytes[_pos++] = (byte)(value >> 16);
			_bytes[_pos++] = (byte)(value >> 24);
			_bytes[_pos++] = (byte)(value >> 32);
			_bytes[_pos++] = (byte)(value >> 40);
			_bytes[_pos++] = (byte)(value >> 48);
			_bytes[_pos++] = (byte)(value >> 56);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray WriteInt64(Int64 value, byte mask) {
            if (_pos + 8 > _capacity) {
                Grow(8);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)((value >> 56) ^ mask);
            _bytes[_pos++] = (byte)((value >> 48) ^ mask);
            _bytes[_pos++] = (byte)((value >> 40) ^ mask);
            _bytes[_pos++] = (byte)((value >> 32) ^ mask);
            _bytes[_pos++] = (byte)((value >> 24) ^ mask);
            _bytes[_pos++] = (byte)((value >> 16) ^ mask);
            _bytes[_pos++] = (byte)((value >> 8) ^ mask);
            _bytes[_pos++] = (byte)(value ^ mask);
#else
            _bytes[_pos++] = (byte)(value ^ mask);
			_bytes[_pos++] = (byte)((value >> 8) ^ mask);
			_bytes[_pos++] = (byte)((value >> 16) ^ mask);
			_bytes[_pos++] = (byte)((value >> 24) ^ mask);
			_bytes[_pos++] = (byte)((value >> 32) ^ mask);
			_bytes[_pos++] = (byte)((value >> 40) ^ mask);
			_bytes[_pos++] = (byte)((value >> 48) ^ mask);
			_bytes[_pos++] = (byte)((value >> 56) ^ mask);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray WriteUInt64(UInt64 value) {
            if (_pos + 8 > _capacity) {
                Grow(8);
            }

#if BLANK_BYTE_ARRAY_ENDIAN_BIG
            _bytes[_pos++] = (byte)(value >> 56);
            _bytes[_pos++] = (byte)(value >> 48);
            _bytes[_pos++] = (byte)(value >> 40);
            _bytes[_pos++] = (byte)(value >> 32);
            _bytes[_pos++] = (byte)(value >> 24);
            _bytes[_pos++] = (byte)(value >> 16);
            _bytes[_pos++] = (byte)(value >> 8);
            _bytes[_pos++] = (byte)value;
#else
			_bytes[_pos++] = (byte)value;
			_bytes[_pos++] = (byte)(value >> 8);
			_bytes[_pos++] = (byte)(value >> 16);
			_bytes[_pos++] = (byte)(value >> 24);
			_bytes[_pos++] = (byte)(value >> 32);
			_bytes[_pos++] = (byte)(value >> 40);
			_bytes[_pos++] = (byte)(value >> 48);
			_bytes[_pos++] = (byte)(value >> 56);
#endif

            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Single ReadFloat() => Convert.ToSingle(ReadUInt32());

        public ByteArray WriteFloat(float value) {
            if (_pos + 4 > _capacity) {
                Grow(4);
            }

            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
                _bytes[_pos++] = bytes[3];
                _bytes[_pos++] = bytes[2];
                _bytes[_pos++] = bytes[1];
                _bytes[_pos++] = bytes[0];
#else
				_bytes[_pos++] = bytes[0];
				_bytes[_pos++] = bytes[1];
				_bytes[_pos++] = bytes[2];
				_bytes[_pos++] = bytes[3];
#endif
            } else {
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
                _bytes[_pos++] = bytes[0];
                _bytes[_pos++] = bytes[1];
                _bytes[_pos++] = bytes[2];
                _bytes[_pos++] = bytes[3];
#else
				_bytes[_pos++] = bytes[3];
				_bytes[_pos++] = bytes[2];
				_bytes[_pos++] = bytes[1];
				_bytes[_pos++] = bytes[0];
#endif
            }

            if (_length < _pos) {
                _length = _pos;
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double ReadDouble() => Convert.ToDouble(ReadUInt64());

        public ByteArray WriteDouble(double value) {
            if (_pos + 8 > _capacity) {
                Grow(8);
            }

            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
                _bytes[_pos++] = bytes[7];
                _bytes[_pos++] = bytes[6];
                _bytes[_pos++] = bytes[5];
                _bytes[_pos++] = bytes[4];
                _bytes[_pos++] = bytes[3];
                _bytes[_pos++] = bytes[2];
                _bytes[_pos++] = bytes[1];
                _bytes[_pos++] = bytes[0];
#else
				_bytes[_pos++] = bytes[0];
				_bytes[_pos++] = bytes[1];
				_bytes[_pos++] = bytes[2];
				_bytes[_pos++] = bytes[3];
				_bytes[_pos++] = bytes[4];
				_bytes[_pos++] = bytes[5];
				_bytes[_pos++] = bytes[6];
				_bytes[_pos++] = bytes[7];
#endif
            } else {
#if BLANK_BYTE_ARRAY_ENDIAN_BIG
                _bytes[_pos++] = bytes[0];
                _bytes[_pos++] = bytes[1];
                _bytes[_pos++] = bytes[2];
                _bytes[_pos++] = bytes[3];
                _bytes[_pos++] = bytes[4];
                _bytes[_pos++] = bytes[5];
                _bytes[_pos++] = bytes[6];
                _bytes[_pos++] = bytes[7];
#else
				_bytes[_pos++] = bytes[7];
				_bytes[_pos++] = bytes[6];
				_bytes[_pos++] = bytes[5];
				_bytes[_pos++] = bytes[4];
				_bytes[_pos++] = bytes[3];
				_bytes[_pos++] = bytes[2];
				_bytes[_pos++] = bytes[1];
				_bytes[_pos++] = bytes[0];
#endif
            }

            if (_length < _pos) {
                _length = _pos;
            }
            return this;
        }

        public string ReadUTF8() {
            int len = ReadUInt16();
            if (len == 0) {
                return "";
            }

            if (len > _length - _pos) {
                throw new Exception("error in ReadUTF8 => read data overflow");
            }

            var t = _pos;
            _pos += len;

            return System.Text.UTF8Encoding.UTF8.GetString(_bytes, t, len);
        }

        public ByteArray WriteUTF8(string value) {
            if (value == null) {
                WriteInt16(0);
            } else {
                var len = System.Text.UTF8Encoding.UTF8.GetByteCount(value);

                if (_pos + len + 2 > _capacity) {
                    Grow(_pos + len + 2 - _capacity);
                }

                if (len > ushort.MaxValue) {
                    throw new Exception("error in WriteUTF8 => target string to large!!");
                }
                WriteInt16(len);
                System.Text.UTF8Encoding.UTF8.GetBytes(value, 0, value.Length, _bytes, _pos);

                _pos += len;
                if (_length < _pos) {
                    _length = _pos;
                }
                //byte[] array = System.Text.UTF8Encoding.UTF8.GetBytes(value);
                //CopyBytesFrom(array, len);
            }

            return this;
        }

        /// <summary>
        /// 截取有效的 字节流
        /// </summary>
        /// <returns></returns>
        public byte[] TruncateValidBytes() {
            var bytes = new byte[Length];

            unsafe {
                fixed (byte* d = bytes, s = _bytes) {
                    Unsafe.CopyBlock(d,s,(uint)Length);
                    // UnsafeUtility.MemCpy(d, s, Length);
                }
            }

            return bytes;
        }

        public ByteArray CopyBytesFrom(byte[] src, int len, int srcPos = 0) {
            if (src == null || srcPos < 0 || len < 0) {
                throw new Exception(string.Format("error in CopyBytesFrom => src = {0}, len = {1}, srcPos = {2}", src, len, srcPos));
            }

            if (_pos + len > _capacity) {
                Grow(_pos + len - _capacity);
            }

            unsafe {
                fixed (byte* d = _bytes, s = src) {
                    Unsafe.CopyBlock(d + _pos, s + srcPos, (uint)len);
                }
            }

            _pos += len;
            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        public ByteArray CopyBytesFromStream(MemoryStream stream) {
            if (stream == null) {
                throw new Exception("error in CopyBytesFromStream => MemoryStream");
            }
            int len = (int)(stream.Length - stream.Position);
            if (_pos + len > _capacity) {
                Grow(_pos + len - _capacity);
            }

            stream.Read(_bytes, 0, len);

            _pos += len;
            if (_length < _pos) {
                _length = _pos;
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteArray CopyBytesFrom(byte[] src) => CopyBytesFrom(src, src.Length);

        public ByteArray CopyBytesTo(byte[] des, int len, int desPos = 0) {
            if (des == null || len < 0 || desPos < 0) {
                throw new Exception(string.Format("error in CopyBytesTo => des = {0}, len = {1}, desPos = {2}", des, len, desPos));
            }

            if (len > _length - _pos) {
                throw new Exception($"error in CopyBytesTo => length is greater than available length. copylen = {len}, pos = {_pos}, length = {_length}");
            }

            unsafe {
                fixed (byte* d = des, s = _bytes) {
                    Unsafe.CopyBlock(d + desPos, s + _pos, (uint)len);
                }
            }
            _pos += len;

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteArray CopyBytesTo(byte[] des) => CopyBytesTo(des, _length - _pos);

        public ByteArray CopyFrom(ByteArray src, int len) {
            if (src == null || len < 0) {
                throw new Exception(string.Format("error in CopyFrom => src = {0}, len = {1}", src, len));
            }

            if (len > src.ByteAvailable) {
                throw new Exception(string.Format("error in CopyFrom => length is greater than available length"));
            }
            CopyBytesFrom(src._bytes, len, src._pos);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteArray CopyFrom(ByteArray src) => CopyFrom(src, src.ByteAvailable);

        public ByteArray CopyTo(ByteArray des, int len) {
            if (des == null) {
                throw new Exception("error in CopyTo => des is null");
            }

            des.CopyFrom(this, len);

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteArray CopyTo(ByteArray des) => CopyTo(des, ByteAvailable);

        public ByteArray Clone() {
            var des = new ByteArray(_capacity);

            unsafe {
                fixed (byte* d = des._bytes, s = _bytes) {
                    Unsafe.CopyBlock(d, s, (uint)_length);
                }
            }

            des._capacity = _capacity;
            des._growthFactor = _growthFactor;
            des._length = _length;
            des._pos = _pos;

            return des;
        }

        public ByteArray TruncateBefore() {
            if (_pos <= 0) {
                return this;
            }

            var newL = _length - _pos;

            if (newL > 0) {
                unsafe {
                    fixed (byte* t = _bytes) {
                        Unsafe.CopyBlock(t, t + _pos, (uint)newL);
                    }
                }

                _pos = 0;
                _length = newL;
            } else {
                Clear();
            }

            return this;
        }
    }
}
