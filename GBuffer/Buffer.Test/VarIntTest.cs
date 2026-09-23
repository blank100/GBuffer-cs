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
	}
}
