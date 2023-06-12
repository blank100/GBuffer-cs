using System.Runtime.CompilerServices;

namespace Gal.Core
{
	/// <summary>
	/// 
	/// </summary>
	/// <para>author gouanlin</para>
	public static class BytesReader
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe sbyte ReadInt8(ref byte* bytes) => (sbyte)*bytes++;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe byte ReadUInt8(ref byte* bytes) => *bytes++;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool ReadBoolean(ref byte* bytes) => *bytes++ != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe short ReadInt16(ref byte* bytes) {
#if BIGENDIAN
			return (short) ((*bytes++ << 8) | *bytes++);
#else
			return (short)(*bytes++ | (*bytes++ << 8));
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ushort ReadUInt16(ref byte* bytes) {
#if BIGENDIAN
			return (ushort) ((*bytes++ << 8) | *bytes++);
#else
			return (ushort)(*bytes++ | (*bytes++ << 8));
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int ReadInt32(ref byte* bytes) {
			var t = *(int*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 4;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe uint ReadUInt32(ref byte* bytes) {
			var t = *(uint*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 4;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long ReadInt64(ref byte* bytes) {
			var t = *(long*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 8;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong ReadUInt64(ref byte* bytes) {
			var t = *(ulong*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 8;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe float ReadFloat(ref byte* bytes) {
			var t = ReadInt32(ref bytes);
			return *(float*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double ReadDouble(ref byte* bytes) {
			var t = ReadInt64(ref bytes);
			return *(double*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe uint ReadVarUInt32(ref byte* bytes) {
			uint value = 0;
			for (var shift = 0; shift < 32; shift += 7) {
				var t = *bytes++;
				value |= (uint)(t & 0b01111111) << shift;
				if ((t & 0b10000000) == 0) {
					break;
				}
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int ReadVarInt32(ref byte* bytes) => ZigZagUtils.DecodeZigZag32(ReadVarUInt32(ref bytes));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong ReadVarUInt64(ref byte* bytes) {
			ulong value = 0;
			for (var shift = 0; shift < 64; shift += 7) {
				var t = *bytes++;
				value |= (ulong)(t & 0b01111111) << shift;
				if ((t & 0b10000000) == 0) {
					break;
				}
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long ReadVarInt64(ref byte* bytes) => ZigZagUtils.DecodeZigZag64(ReadVarUInt64(ref bytes));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe string ReadUtf8(ref byte* bytes, int len) {
			if (len == 0) return string.Empty;
			var t = System.Text.Encoding.UTF8.GetString(bytes, len);
			bytes += len;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe string ReadUtf8(ref byte* bytes) => ReadUtf8(ref bytes, ReadUInt16(ref bytes));
	}
}