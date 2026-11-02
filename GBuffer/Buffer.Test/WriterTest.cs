using System.Buffers;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class WriterTest {
		[Fact]
		public void WriteOverloadsAppendInOrder() {
			using var w = new Writer<int>(8);

			w.Write(1);
			w.Write(2, 3);
			w.Write(4, 5, 6);
			w.Write(7, 8, 9, 10);
			w.Write(11, 12, 13, 14, 15);
			w.Write(16, 17, 18, 19, 20, 21);

			Assert.Equal(21, w.Length);
			Assert.Equal(21, w.Position);
			Assert.Equal(21, w.WrittenSpan.Length);
			for (var i = 0; i < 21; i++) Assert.Equal(i + 1, w.WrittenSpan[i]);
		}

		[Fact]
		public void WriteSpanMemoryAndSequence() {
			using var w = new Writer<int>(16);
			var arr = new[] { 1, 2, 3 };

			w.Write(arr.AsSpan());
			w.Write(arr.AsMemory());
			w.Write(new ReadOnlySequence<int>(arr));

			Assert.Equal(9, w.Length);
			var expected = new[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
			Assert.Equal(expected, w.WrittenSpan.ToArray());
		}

		[Fact]
		public void GetSpanAndHintSizeExpandBuffer() {
			using var w = new Writer<byte>(4);

			var span = w.GetSpan(100);
			Assert.True(span.Length >= 100);
			Assert.True(w.Capacity >= 100);

			w.HintSize(500);
			Assert.True(w.Capacity - w.Position >= 500);

			var mem = w.GetMemory(1000);
			Assert.True(mem.Length >= 1000);
		}

		[Fact]
		public void ClearResetsPositionAndLength() {
			using var w = new Writer<byte>(16);
			w.WriteInt32(0x01020304);

			w.Clear();
			Assert.Equal(0, w.Length);
			Assert.Equal(0, w.Position);

			w.WriteInt8(9);
			Assert.Equal(1, w.Length);
			Assert.Equal(9, w.WrittenSpan[0]);
		}

		[Fact]
		public void DiscardMovesRemainingDataToFront() {
			using var w = new Writer<byte>(32);
			w.Write("0123456789".Select(c => (byte) c).ToArray().AsSpan());
			w.Position = 4;

			w.Discard();

			Assert.Equal(6, w.Length);
			Assert.Equal(0, w.Position);
			Assert.Equal("456789", System.Text.Encoding.ASCII.GetString(w.WrittenSpan));
		}

		[Fact]
		public void LengthSetterGrowsBufferAndResetsPositionWhenShrinking() {
			using var w = new Writer<byte>(16);
			w.WriteInt32(1);

			w.Length = 5000;
			Assert.Equal(5000, w.Length);
			Assert.True(w.Capacity >= 5000);

			w.Length = 0;
			Assert.Equal(0, w.Length);
			Assert.Equal(0, w.Position);
		}

		[Fact]
		public void IndexerAndRawArrayExposeBackingArray() {
			using var w = new Writer<byte>(16);
			w[3] = 65;
			Assert.Equal(65, w.RawArray[3]);
		}

		[Fact]
		public void GrowDoublesCapacity() {
			using var w = new Writer<byte>(1024);
			for (var i = 0; i < 1024; i++) w.Write((byte) 1);
			Assert.Equal(1024, w.Capacity);

			w.Write((byte) 1); // 触发扩容
			Assert.Equal(2048, w.Capacity);
			Assert.Equal(1025, w.Length);
		}
	}
}
