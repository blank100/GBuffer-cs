using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class RefStackTest {
		[Fact]
		public void PushPopPeekLifoOrder() {
			Span<int> buffer = stackalloc int[4];
			var stack = new RefStack<int>(buffer);

			stack.Push(1);
			stack.Push(2);
			stack.Push(3);

			Assert.Equal(3, stack.Count);
			Assert.Equal(3, stack.Peek());
			Assert.Equal(3, stack.Count);

			Assert.Equal(3, stack.Pop());
			Assert.Equal(2, stack.Pop());
			Assert.Equal(1, stack.Pop());
			Assert.Equal(0, stack.Count);
		}

		[Fact]
		public void GrowsWhenCapacityExceeded() {
			Span<int> buffer = stackalloc int[2];
			var stack = new RefStack<int>(buffer);

			for (var i = 1; i <= 10; i++) stack.Push(i);
			Assert.Equal(10, stack.Count);

			for (var i = 10; i >= 1; i--) Assert.Equal(i, stack.Pop());
			Assert.Equal(0, stack.Count);
		}

		[Fact]
		public void ClearResetsCount() {
			Span<int> buffer = stackalloc int[8];
			var stack = new RefStack<int>(buffer);
			stack.Push(1);
			stack.Push(2);

			stack.Clear();

			Assert.Equal(0, stack.Count);
			stack.Push(42);
			Assert.Equal(42, stack.Peek());
		}
	}
}
