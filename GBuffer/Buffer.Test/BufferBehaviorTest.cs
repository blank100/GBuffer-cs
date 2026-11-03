using Gal.Core;

using Xunit;

namespace Serialize.Test {
	/// <summary>
	/// Buffer&lt;byte&gt; 行为补充：Clear 复用、空字符串、全类型混合 roundtrip。
	/// </summary>
	public class BufferBehaviorTest {
		[Fact]
		public void ClearThenReuse() {
			using var buffer = new Buffer<byte>(64);
			buffer.WriteInt32(123);
			buffer.Clear();

			Assert.Equal(0, buffer.Length);
			Assert.Equal(0, buffer.Position);

			buffer.WriteInt32(456);
			buffer.Position = 0;
			Assert.Equal(456, buffer.ReadInt32());
		}

		[Fact]
		public void WriteUtf8EmptyAndNull() {
			using var buffer = new Buffer<byte>(16);
			buffer.WriteUtf8(null);
			buffer.WriteUtf8(string.Empty);

			Assert.Equal(4, buffer.Length); // 两个 UInt16 长度前缀(0)

			buffer.Position = 0;
			Assert.Equal(string.Empty, buffer.ReadUtf8());
			Assert.Equal(string.Empty, buffer.ReadUtf8());
		}

		[Fact]
		public void MixedTypesRoundTripInOrder() {
			using var buffer = new Buffer<byte>(256);

			buffer.WriteBoolean(true);
			buffer.WriteInt8(-8);
			buffer.WriteUInt8(250);
			buffer.WriteInt16(-1234);
			buffer.WriteUInt16(65000);
			buffer.WriteInt32(int.MinValue);
			buffer.WriteUInt32(uint.MaxValue);
			buffer.WriteInt64(long.MinValue);
			buffer.WriteUInt64(ulong.MaxValue);
			buffer.WriteFloat(2.5f);
			buffer.WriteDouble(-3.75);
			buffer.WriteVarInt32(-100);
			buffer.WriteVarUInt64(ulong.MaxValue);
			buffer.WriteUtf8("混合 text 🚀");

			buffer.Position = 0;
			Assert.True(buffer.ReadBoolean());
			Assert.Equal((sbyte) -8, buffer.ReadInt8());
			Assert.Equal((byte) 250, buffer.ReadUInt8());
			Assert.Equal((short) -1234, buffer.ReadInt16());
			Assert.Equal((ushort) 65000, buffer.ReadUInt16());
			Assert.Equal(int.MinValue, buffer.ReadInt32());
			Assert.Equal(uint.MaxValue, buffer.ReadUInt32());
			Assert.Equal(long.MinValue, buffer.ReadInt64());
			Assert.Equal(ulong.MaxValue, buffer.ReadUInt64());
			Assert.Equal(2.5f, buffer.ReadFloat());
			Assert.Equal(-3.75, buffer.ReadDouble());
			Assert.Equal(-100, buffer.ReadVarInt32());
			Assert.Equal(ulong.MaxValue, buffer.ReadVarUInt64());
			Assert.Equal("混合 text 🚀", buffer.ReadUtf8());
			Assert.Equal(0, buffer.ReadableCount);
		}
	}
}
