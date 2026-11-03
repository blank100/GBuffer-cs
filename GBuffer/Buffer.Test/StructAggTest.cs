using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	/// <summary>
	/// unmanaged 聚合读写（Write&lt;TValue&gt;/Read&lt;TValue&gt;）测试：Pack=1 布局与 SizeOf、
	/// Buffer&lt;byte&gt; 写+读 roundtrip、Reader&lt;byte&gt; 读 roundtrip、与标量/Utf8 混写顺序。
	/// </summary>
	public class StructAggTest {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MsgHeader {
			public int Id;
			public long Timestamp;
			public ushort Type;
		}

		private static MsgHeader Sample => new MsgHeader { Id = 0x01020304, Timestamp = -1234567890123L, Type = 0xBEEF };

		[Fact]
		public void SizeOfPack1Test() {
			Assert.Equal(14, Unsafe.SizeOf<MsgHeader>());
			Assert.Equal(14, Marshal.SizeOf<MsgHeader>());
		}

		[Fact]
		public void BufferWriteReadRoundtripTest() {
			using var buffer = new Buffer<byte>(64);
			var src = Sample;
			buffer.Write(src);
			Assert.Equal(14, buffer.Position);

			buffer.Position = 0;
			var dst = buffer.Read<MsgHeader>();
			Assert.Equal(src.Id, dst.Id);
			Assert.Equal(src.Timestamp, dst.Timestamp);
			Assert.Equal(src.Type, dst.Type);
			Assert.Equal(14, buffer.Position);
		}

		[Fact]
		public void ReaderReadRoundtripTest() {
			using var buffer = new Buffer<byte>(64);
			var src = Sample;
			buffer.Write(src);

			var reader = new Reader<byte>(buffer.WrittenSpan.ToArray());
			var dst = reader.Read<MsgHeader>();
			Assert.Equal(src.Id, dst.Id);
			Assert.Equal(src.Timestamp, dst.Timestamp);
			Assert.Equal(src.Type, dst.Type);
			Assert.Equal(14, reader.Position);
		}

		[Fact]
		public void MixedWithScalarAndUtf8Test() {
			using var buffer = new Buffer<byte>(64);
			var src = Sample;
			buffer.WriteInt32(0x11223344);
			buffer.Write(src);
			buffer.WriteUtf8("hi");

			buffer.Position = 0;
			Assert.Equal(0x11223344, buffer.ReadInt32());
			var dst = buffer.Read<MsgHeader>();
			Assert.Equal(src.Id, dst.Id);
			Assert.Equal(src.Timestamp, dst.Timestamp);
			Assert.Equal(src.Type, dst.Type);
			Assert.Equal("hi", buffer.ReadUtf8());
			Assert.Equal(22, buffer.Position); // 4(Int32) + 14(聚合) + 2(UInt16 长度) + 2("hi")
		}

		[Fact]
		public void ReadGuardTest() {
			// Reader<byte> 底层是精确数组：剩余 4 < 14，抛 EndOfStreamException（与标量读一致，不会越界读）
			var reader = new Reader<byte>(new byte[4]);
			Assert.Throws<System.IO.EndOfStreamException>(() => reader.Read<MsgHeader>());
			// Buffer<byte> 底层经 ArrayPool.Rent，Span 长度受池容量限制，守卫语义与既有标量读相同
		}
	}
}
