using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Gal.Core {
    public static class Writer2ByteEx {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteInt8<TSelf>(this TSelf self, sbyte value) where TSelf : IWriter2<TSelf, byte> {
            self.Write((byte)value);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteUInt8<TSelf>(this TSelf self, byte value) where TSelf : IWriter2<TSelf, byte> {
            self.Write(value);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteBoolean<TSelf>(this TSelf self, bool value) where TSelf : IWriter2<TSelf, byte> {
            self.Write(value ? (byte)1 : (byte)0);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteUInt16<TSelf>(this TSelf self, UInt16 value) where TSelf : IWriter2<TSelf, byte> {
            var span = self.GetSpan(2);
            span[1] = (byte)(value >> 8);
            span[0] = (byte)value;
            self.Advance(2);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteInt16<TSelf>(this TSelf self, Int16 value) where TSelf : IWriter2<TSelf, byte> {
            var span = self.GetSpan(2);
            span[1] = (byte)(value >> 8);
            span[0] = (byte)value;
            self.Advance(2);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteInt32<TSelf>(this TSelf self, int v) where TSelf : IWriter2<TSelf, byte> {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self.GetSpan(4)), /*!BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(v) :*/ v);
            self.Advance(4);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteUInt32<TSelf>(this TSelf self, UInt32 value) where TSelf : IWriter2<TSelf, byte> {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self.GetSpan(4)), !BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
            self.Advance(4);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteInt64<TSelf>(this TSelf self, Int64 value) where TSelf : IWriter2<TSelf, byte> {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self.GetSpan(8)), !BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
            self.Advance(8);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteUInt64<TSelf>(this TSelf self, UInt64 value) where TSelf : IWriter2<TSelf, byte> {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self.GetSpan(8)), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
            self.Advance(8);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TSelf WriteFloat<TSelf>(this TSelf self, float value) where TSelf : IWriter2<TSelf, byte> => self.WriteInt32(*(int*)&value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TSelf WriteDouble<TSelf>(this TSelf self, double value) where TSelf : IWriter2<TSelf, byte> => self.WriteInt64(*(long*)&value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteVarUInt32<TSelf>(this TSelf self, uint value) where TSelf : IWriter2<TSelf, byte> {
            var span = self.GetSpan(5);
            self.Advance(SpanByteUtils.WriteVarUInt32(ref span, value));
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteVarInt32<TSelf>(this TSelf self, int value) where TSelf : IWriter2<TSelf, byte> => self.WriteVarUInt32(ZigZagUtils.EncodeZigZag32(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteVarUInt64<TSelf>(this TSelf self, ulong value) where TSelf : IWriter2<TSelf, byte> {
            Span<byte> buffer = stackalloc byte[10];
            var e = SpanByteUtils.WriteVarUInt64(ref buffer, value);
            self.HintSize(e);
            buffer[..e].CopyTo(self.Span);
            self.Advance(e);
            return self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSelf WriteVarInt64<TSelf>(this TSelf self, long value) where TSelf : IWriter2<TSelf, byte> {
            self.WriteVarUInt64(ZigZagUtils.EncodeZigZag64(value));
            return self;
        }

        public static unsafe TSelf WriteUtf8<TSelf>(this TSelf self, string value) where TSelf : IWriter2<TSelf, byte> {
            if (string.IsNullOrEmpty(value)) {
                self.WriteInt16(0);
                return self;
            }

            ReadOnlySpan<char> chars = value;
            var maxLength = chars.Length * 3;

            if (self.WritableCount >= maxLength + 2) {
                var count = System.Text.Encoding.UTF8.GetBytes(chars, self.Span[2..]);
                if (count > short.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
                self.WriteUInt16((ushort)count);
                self.Advance(count);
            } else {
                var count = System.Text.Encoding.UTF8.GetByteCount(chars);
                self.WriteUInt16((ushort)count);
                var span = self.GetSpan(count);
                System.Text.Encoding.UTF8.GetBytes(chars, span);
                self.Advance(count);
            }

            return self;
        }
    }
}
