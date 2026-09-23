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
		public void Sleb128Int32MatchesReferenceEncoding() {
			AssertSleb128Int32Encoding(0, 0x00);
			AssertSleb128Int32Encoding(1, 0x01);
			AssertSleb128Int32Encoding(-1, 0x7F);
			AssertSleb128Int32Encoding(63, 0x3F);
			AssertSleb128Int32Encoding(64, 0xC0, 0x00);
			AssertSleb128Int32Encoding(-64, 0x40);
			AssertSleb128Int32Encoding(-65, 0xBF, 0x7F);
			AssertSleb128Int32Encoding(127, 0xFF, 0x00);
			AssertSleb128Int32Encoding(128, 0x80, 0x01);
			AssertSleb128Int32Encoding(-128, 0x80, 0x7F);
			AssertSleb128Int32Encoding(int.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF, 0x07);
			AssertSleb128Int32Encoding(int.MinValue, 0x80, 0x80, 0x80, 0x80, 0x78);
		}

		[Fact]
		public void Sleb128Int64MatchesReferenceEncoding() {
			AssertSleb128Int64Encoding(0, 0x00);
			AssertSleb128Int64Encoding(1, 0x01);
			AssertSleb128Int64Encoding(-1, 0x7F);
			AssertSleb128Int64Encoding(63, 0x3F);
			AssertSleb128Int64Encoding(64, 0xC0, 0x00);
			AssertSleb128Int64Encoding(-64, 0x40);
			AssertSleb128Int64Encoding(-65, 0xBF, 0x7F);
			AssertSleb128Int64Encoding(127, 0xFF, 0x00);
			AssertSleb128Int64Encoding(128, 0x80, 0x01);
			AssertSleb128Int64Encoding(-128, 0x80, 0x7F);
			AssertSleb128Int64Encoding(long.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00);
			AssertSleb128Int64Encoding(long.MinValue, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x7F);
		}

		[Fact]
		public void Sleb128ReaderAdvancesExactly() {
			var reader = new Reader<byte>(new byte[] { 0x80, 0x01, 0x7F });

			Assert.Equal(128, reader.ReadSleb128Int32());
			Assert.Equal(2, reader.Position);
			Assert.Equal(-1, reader.ReadSleb128Int32());
			Assert.Equal(3, reader.Position);
		}

		[Fact]
		public void Sleb128RejectsMalformedSequences() {
			var int32Unterminated = new Reader<byte>(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80 });
			var int32Overflow = new Reader<byte>(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x08 });
			var int64Unterminated = new Reader<byte>(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 });
			var int64InvalidTenthByte = new Reader<byte>(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 });

			Assert.Throws<FormatException>(() => int32Unterminated.ReadSleb128Int32());
			Assert.Throws<FormatException>(() => int32Overflow.ReadSleb128Int32());
			Assert.Throws<FormatException>(() => int64Unterminated.ReadSleb128Int64());
			Assert.Throws<FormatException>(() => int64InvalidTenthByte.ReadSleb128Int64());
		}

		private static void AssertSleb128Int32Encoding(int value, params byte[] expected) {
			Span<byte> bytes = stackalloc byte[5];
			var written = SpanByteUtils.WriteSleb128Int32(bytes, value);

			Assert.Equal(expected, bytes[..written].ToArray());
			Assert.Equal(value, SpanByteUtils.ReadSleb128Int32(bytes, out var read));
			Assert.Equal(written, read);
		}

		private static void AssertSleb128Int64Encoding(long value, params byte[] expected) {
			Span<byte> bytes = stackalloc byte[10];
			var written = SpanByteUtils.WriteSleb128Int64(bytes, value);

			Assert.Equal(expected, bytes[..written].ToArray());
			Assert.Equal(value, SpanByteUtils.ReadSleb128Int64(bytes, out var read));
			Assert.Equal(written, read);
		}
	}
}
