using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class SpanByteUtilsTest {
		[Fact]
		public void FixedWidthRoundTripsLittleEndianByDefault() {
			Span<byte> bytes = stackalloc byte[8];

			Assert.Equal(4, SpanByteUtils.WriteInt32(bytes, 0x01020304));
			Assert.Equal(new byte[] { 0x04, 0x03, 0x02, 0x01 }, bytes.Slice(0, 4).ToArray());
			Assert.Equal(0x01020304, SpanByteUtils.ReadInt32(bytes));
		}

		[Fact]
		public void FixedWidthCanUseBigEndian() {
			Span<byte> bytes = stackalloc byte[8];

			Assert.Equal(4, SpanByteUtils.WriteInt32(bytes, 0x01020304, true));
			Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, bytes.Slice(0, 4).ToArray());
			Assert.Equal(0x01020304, SpanByteUtils.ReadInt32(bytes, true));
		}

		[Fact]
		public void Utf8RoundTripsWithLengthPrefix() {
			var value = "text";
			Span<byte> bytes = stackalloc byte[16];

			var written = SpanByteUtils.WriteUtf8(bytes, value);
			var result = SpanByteUtils.ReadUtf8(bytes, out var read);

			Assert.Equal(value, result);
			Assert.Equal(written, read);
			Assert.Equal(written, 2 + bytes[0]);
		}

		[Fact]
		public void UInt64RoundTripsAcrossReaderWriterVariants() {
			const ulong value = 0x0102030405060708UL;

			using var buffer = new Buffer<byte>(16);
			buffer.WriteUInt64(value);
			buffer.Position = 0;
			Assert.Equal(value, buffer.ReadUInt64());

			Span<byte> bytes = stackalloc byte[8];
			var writer = new RefWriter<byte>(bytes);
			writer.WriteUInt64(value);

			var refReader = new RefReader<byte>(bytes);
			Assert.Equal(value, refReader.ReadUInt64());

			var reader = new Reader<byte>(bytes.ToArray());
			Assert.Equal(value, reader.ReadUInt64());
		}
	}
}
