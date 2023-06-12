using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class RefBytesReaderTest {
		[Fact]
		public void Test() {
			{
				using var buffer = new Buffer<byte>(1024);
				buffer.Write((byte) '\r');
				buffer.WriteInt8(127);
				buffer.WriteUInt8(128);
				buffer.WriteUInt8(255);

				var reader = new RefReader<byte>(buffer.writtenSpan);
				Assert.Equal((sbyte) '\r', reader.ReadInt8());
				Assert.Equal(127,          reader.ReadInt8());
				Assert.Equal(128,          reader.ReadUInt8());
				Assert.Equal(255,          reader.ReadUInt8());
			}

			{
				using var buffer = new Buffer<byte>(1024);
				buffer.WriteInt16(32767);
				buffer.WriteUInt16(32768);

				var reader = new RefReader<byte>(buffer.writtenSpan);
				Assert.Equal(32767, reader.ReadInt16());
				Assert.Equal(32768, reader.ReadUInt16());
			}

			{
				using var buffer = new Buffer<byte>(1024);
				buffer.WriteInt32(int.MaxValue);
				buffer.WriteUInt32(uint.MaxValue);

				var reader = new RefReader<byte>(buffer.writtenSpan);
				Assert.Equal(int.MaxValue,  reader.ReadInt32());
				Assert.Equal(uint.MaxValue, reader.ReadUInt32());
			}

			{
				using var buffer = new Buffer<byte>(1024);
				buffer.WriteInt64(long.MaxValue);
				buffer.WriteInt64(long.MinValue);
				buffer.WriteUInt64(ulong.MaxValue);
				buffer.WriteUInt64(ulong.MinValue);

				var reader = new RefReader<byte>(buffer.writtenSpan);
				Assert.Equal(long.MaxValue,  reader.ReadInt64());
				Assert.Equal(long.MinValue,  reader.ReadInt64());
				Assert.Equal(ulong.MaxValue, reader.ReadUInt64());
				Assert.Equal(ulong.MinValue, reader.ReadUInt64());
			}

			{
				const string text1 = "this is string";
				const string text2 = "这是文本";

				using var buffer = new Buffer<byte>(1024);
				buffer.WriteUtf8(text1);
				buffer.WriteUtf8(text2);

				var reader = new RefReader<byte>(buffer.writtenSpan);
				Assert.Equal(text1, reader.ReadUtf8());
				Assert.Equal(text2, reader.ReadUtf8());
			}
		}
	}
}