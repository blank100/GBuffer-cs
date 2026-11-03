using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class CircularBufferEdgeTest {
		[Fact]
		public void DequeueOnEmptyThrows() {
			var buffer = new CircularBuffer<int>(2);
			Assert.Throws<InvalidOperationException>(() => buffer.Dequeue());
		}

		[Fact]
		public void TryDequeueOnEmptyReturnsFalse() {
			var buffer = new CircularBuffer<int>(2);
			Assert.False(buffer.TryDequeue(out var item));
			Assert.Equal(default, item);
		}

		[Fact]
		public void WrapAroundManyCyclesKeepsOrder() {
			var buffer = new CircularBuffer<int>(4);
			for (var round = 0; round < 100; round++) {
				for (var i = 0; i < 4; i++) buffer.Enqueue(round * 10 + i);
				Assert.True(buffer.IsFull);
				for (var i = 0; i < 4; i++) Assert.Equal(round * 10 + i, buffer.Dequeue());
				Assert.True(buffer.IsEmpty);
			}
		}
	}
}
