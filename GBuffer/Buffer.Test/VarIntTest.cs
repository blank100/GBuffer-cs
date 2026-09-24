using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class VarIntTest {
		[Fact]
		public void ReadVarUInt32AcceptsMaximumValue() {
			var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F };
			var reader = new Reader<byte>(bytes);

			Assert.Equal(uint.MaxValue, reader.ReadVarUInt32());
			Assert.Equal(bytes.Length, reader.Position);
		}

		[Fact]
		public void ReadVarUInt32RejectsFifthByteOverflow() {
			var reader = new Reader<byte>(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x10 });

			Assert.Throws<FormatException>(() => reader.ReadVarUInt32());
		}

		[Fact]
		public void ReadVarUInt32RejectsUnterminatedSlowPath() {
			var reader = new Reader<byte>(new byte[] { 0x80, 0x80 });

			Assert.Throws<FormatException>(() => reader.ReadVarUInt32());
		}

		[Fact]
		public void ReadVarUInt64AcceptsMaximumValue() {
			var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };
			var reader = new Reader<byte>(bytes);

			Assert.Equal(ulong.MaxValue, reader.ReadVarUInt64());
			Assert.Equal(bytes.Length, reader.Position);
		}

		[Fact]
		public void ReadVarUInt64RejectsTenthByteOverflow() {
			var reader = new Reader<byte>(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 });

			Assert.Throws<FormatException>(() => reader.ReadVarUInt64());
		}

		[Fact]
		public void ReadVarUInt64RejectsUnterminatedSlowPath() {
			var reader = new Reader<byte>(new byte[] { 0x80, 0x80, 0x80 });

			Assert.Throws<FormatException>(() => reader.ReadVarUInt64());
		}

		[Fact]
		public void VarIntSizeMatchesWrittenLength() {
			var uintValues = new uint[] { 0, 127, 128, 16383, 16384, uint.MaxValue };
			var intValues = new int[] { int.MinValue, -1, 0, 1, int.MaxValue };
			var ulongValues = new ulong[] { 0, 127, 128, 16383, 16384, ulong.MaxValue };
			var longValues = new long[] { long.MinValue, -1, 0, 1, long.MaxValue };

			Span<byte> bytes = stackalloc byte[10];
			foreach (var value in uintValues)
				Assert.Equal(SpanByteUtils.GetVarUInt32Size(value), SpanByteUtils.WriteVarUInt32(bytes, value));
			foreach (var value in intValues)
				Assert.Equal(SpanByteUtils.GetVarInt32Size(value), SpanByteUtils.WriteVarInt32(bytes, value));
			foreach (var value in ulongValues)
				Assert.Equal(SpanByteUtils.GetVarUInt64Size(value), SpanByteUtils.WriteVarUInt64(bytes, value));
			foreach (var value in longValues)
				Assert.Equal(SpanByteUtils.GetVarInt64Size(value), SpanByteUtils.WriteVarInt64(bytes, value));
		}

		[Fact]
		public void VarIntWriteAcceptsExactSpan() {
			var uintValues = new uint[] { 0, 127, 128, 16383, 16384, uint.MaxValue };
			var intValues = new int[] { int.MinValue, -1, 0, 1, int.MaxValue };
			var ulongValues = new ulong[] { 0, 127, 128, 16383, 16384, ulong.MaxValue };
			var longValues = new long[] { long.MinValue, -1, 0, 1, long.MaxValue };
			Span<byte> bytes = stackalloc byte[10];

			foreach (var value in uintValues) {
				var target = bytes[..SpanByteUtils.GetVarUInt32Size(value)];
				var written = SpanByteUtils.WriteVarUInt32(target, value);
				Assert.Equal(value, SpanByteUtils.ReadVarUInt32(target, out var read));
				Assert.Equal(written, read);
			}

			foreach (var value in intValues) {
				var target = bytes[..SpanByteUtils.GetVarInt32Size(value)];
				var written = SpanByteUtils.WriteVarInt32(target, value);
				Assert.Equal(value, SpanByteUtils.ReadVarInt32(target, out var read));
				Assert.Equal(written, read);
			}

			foreach (var value in ulongValues) {
				var target = bytes[..SpanByteUtils.GetVarUInt64Size(value)];
				var written = SpanByteUtils.WriteVarUInt64(target, value);
				Assert.Equal(value, SpanByteUtils.ReadVarUInt64(target, out var read));
				Assert.Equal(written, read);
			}

			foreach (var value in longValues) {
				var target = bytes[..SpanByteUtils.GetVarInt64Size(value)];
				var written = SpanByteUtils.WriteVarInt64(target, value);
				Assert.Equal(value, SpanByteUtils.ReadVarInt64(target, out var read));
				Assert.Equal(written, read);
			}
		}

		[Fact]
		public void RefWriterVarIntGrowsWhenNeeded() {
			Span<byte> initial = stackalloc byte[1];
			var writer = new RefWriter<byte>(initial);

			writer.WriteVarUInt32(uint.MaxValue);
			writer.WriteVarInt64(long.MinValue);

			var bytes = writer.WrittenSpan.ToArray();
			writer.Dispose();

			var reader = new Reader<byte>(bytes);
			Assert.Equal(uint.MaxValue, reader.ReadVarUInt32());
			Assert.Equal(long.MinValue, reader.ReadVarInt64());
		}

	}
}
