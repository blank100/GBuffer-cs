using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gal.Core
{
	/// <summary>
	/// 
	/// </summary>
	/// <para>author gouanlin</para>
	public static class SpanByteWriterExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteUInt8(this ref Span<byte> self, byte value) {
			self[0] = value;
			self = self[1..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteInt8(this ref Span<byte> self, sbyte value) {
			self[0] = (byte)value;
			self = self[1..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteInt16(this ref Span<byte> self, int value) {
#if BIGENDIAN
			self[1] = (byte) (value >> 8);
			self[0] = (byte) value;
#else
			self[0] = (byte)value;
			self[1] = (byte)(value >> 8);
#endif

			self = self[2..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteInt32(this ref Span<byte> self, int value) {
#if BIGENDIAN
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), BinaryPrimitives.ReverseEndianness(value));
#else
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
#endif
			self = self[4..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteUInt32(this ref Span<byte> self, uint value) {
#if BIGENDIAN
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), BinaryPrimitives.ReverseEndianness(value));
#else
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
#endif
			self = self[4..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteInt64(this ref Span<byte> self, long value) {
#if BIGENDIAN
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), BinaryPrimitives.ReverseEndianness(value));
#else
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
#endif
			self = self[8..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteUInt64(this ref Span<byte> self, ulong value) {
#if BIGENDIAN
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), BinaryPrimitives.ReverseEndianness(value));
#else
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
#endif
			self = self[8..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteVarUInt32(this ref Span<byte> self, uint value) {
			var i = 0;
			while (value >= 0b10000000) {
				self[i++] = (byte)(value | 0b10000000);
				value >>= 7;
			}
			self[i++] = (byte)value;
			self = self[i..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteVarInt32(this ref Span<byte> self, int value) => self.WriteVarUInt32(ZigZagUtils.EncodeZigZag32(value));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteVarUInt64(this ref Span<byte> self, ulong value) {
			var i = 0;
			while (value >= 0b10000000) {
				self[i++] = (byte)(value | 0b10000000);
				value >>= 7;
			}
			self[i++] = (byte)value;
			self = self[i..];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteVarInt64(this ref Span<byte> self, long value) => self.WriteVarUInt64(ZigZagUtils.EncodeZigZag64(value));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteFloat(this ref Span<byte> self, float value) {
			self.WriteInt32(*(int*)&value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteDouble(this ref Span<byte> self, double value) {
			self.WriteInt64(*(long*)&value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void WriteUtf8(this ref Span<byte> self, string value) {
			if (string.IsNullOrEmpty(value)) {
				self.WriteInt16(0);
				return;
			}
			var bytesCount = System.Text.Encoding.UTF8.GetByteCount(value!);
			self.WriteInt16((short)bytesCount);
			fixed (char* source = value) {
				fixed (byte* target = self) {
					System.Text.Encoding.UTF8.GetBytes(source, value.Length, target, bytesCount);
				}
			}
			self = self[bytesCount..];
		}
	}
}