using Gal.Core;

using Xunit;

namespace Serialize.Test {
	/// <summary>
	/// 指针版 BytesWriter/BytesReader 的补充覆盖：Boolean/UInt8/泛型 Write&lt;T&gt;/不带长度的 Utf8/超长异常。
	/// </summary>
	public class BytesWriterExtraTest {
		[Fact]
		public unsafe void BooleanAndUInt8Test() {
			var bytes = new byte[8];
			fixed (byte* p = bytes) {
				var w = p;
				BytesWriter.WriteBoolean(ref w, true);
				BytesWriter.WriteBoolean(ref w, false);
				BytesWriter.WriteUInt8(ref w, 200);
				BytesWriter.WriteInt8(ref w, -100);

				var r = p;
				Assert.True(BytesReader.Read<byte>(ref r) != 0);
				Assert.False(BytesReader.Read<byte>(ref r) != 0);
				Assert.Equal((byte) 200, BytesReader.Read<byte>(ref r));
				Assert.Equal((sbyte) -100, BytesReader.Read<sbyte>(ref r));
			}
		}

		[Fact]
		public unsafe void GenericWriteReadTest() {
			var bytes = new byte[8];
			fixed (byte* p = bytes) {
				var w = p;
				BytesWriter.Write<ushort>(ref w, 0xBEEF);
				BytesWriter.Write<int>(ref w, -123456);

				var r = p;
				Assert.Equal((ushort) 0xBEEF, BytesReader.Read<ushort>(ref r));
				Assert.Equal(-123456, BytesReader.Read<int>(ref r));
			}
		}

		[Fact]
		public unsafe void Utf8WithoutLengthTest() {
			var bytes = new byte[64];
			fixed (byte* p = bytes) {
				var w = p;
				BytesWriter.WriteUtf8WithoutLength(ref w, "hello");

				var r = p;
				Assert.Equal("hello", BytesReader.ReadUtf8(ref r, 5));
			}
		}

		[Fact]
		public unsafe void WriteUtf8RejectsOverlongString() {
			var bytes = new byte[8];
			fixed (byte* p = bytes) {
				var w = p;
				Assert.Throws<ArgumentOutOfRangeException>(() => BytesWriter.WriteUtf8(ref w, new string('a', 40000)));
			}
		}
	}
}
