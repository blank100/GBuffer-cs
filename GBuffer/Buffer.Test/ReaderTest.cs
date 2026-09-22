using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class ReaderTest {
		[Fact]
		public void SpanMethodsReturnViews() {
			var data = new byte[] { 1, 2, 3, 4 };
			var reader = new Reader<byte>(data) { Position = 1 };

			var span = reader.Span;
			var getSpan = reader.GetSpan(2);

			data[1] = 9;
			data[2] = 8;

			Assert.Equal(9, span[0]);
			Assert.Equal(8, getSpan[1]);
		}

		[Fact]
		public void MemoryMethodsReturnViews() {
			var data = new byte[] { 1, 2, 3, 4 };
			var reader = new Reader<byte>(data) { Position = 1 };

			var memory = reader.Memory;
			var getMemory = reader.GetMemory(2);

			data[1] = 9;
			data[2] = 8;

			Assert.Equal(9, memory.Span[0]);
			Assert.Equal(8, getMemory.Span[1]);
		}

		[Fact]
		public void BufferReaderSpanMethodsReturnViews() {
			using var buffer = new Buffer<byte>(8);
			IReader<byte> reader = buffer;

			buffer.Write((byte) 1, (byte) 2, (byte) 3, (byte) 4);
			reader.Position = 1;

			var span = reader.Span;
			var getSpan = reader.GetSpan(2);

			buffer.RawArray[1] = 9;
			buffer.RawArray[2] = 8;

			Assert.Equal(9, span[0]);
			Assert.Equal(8, getSpan[1]);
		}

		[Fact]
		public void BufferReaderMemoryMethodsReturnViews() {
			using var buffer = new Buffer<byte>(8);
			IReader<byte> reader = buffer;

			buffer.Write((byte) 1, (byte) 2, (byte) 3, (byte) 4);
			reader.Position = 1;

			var memory = reader.Memory;
			var getMemory = reader.GetMemory(2);

			buffer.RawArray[1] = 9;
			buffer.RawArray[2] = 8;

			Assert.Equal(9, memory.Span[0]);
			Assert.Equal(8, getMemory.Span[1]);
		}
	}
}
