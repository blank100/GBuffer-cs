using System.Runtime.CompilerServices;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class CircularBufferTest {
		[Fact]
		public void ConstructorRejectsNonPositiveCapacity() {
			Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(-1));
		}

		[Fact]
		public void EnqueueDequeuePreservesOrder() {
			var buffer = new CircularBuffer<int>(3);

			Assert.True(buffer.TryEnqueue(1));
			Assert.True(buffer.TryEnqueue(2));
			Assert.True(buffer.TryEnqueue(3));
			Assert.True(buffer.IsFull);

			Assert.True(buffer.TryDequeue(out var first));
			Assert.True(buffer.TryDequeue(out var second));
			Assert.True(buffer.TryDequeue(out var third));
			Assert.Equal(1, first);
			Assert.Equal(2, second);
			Assert.Equal(3, third);
			Assert.True(buffer.IsEmpty);
		}

		[Fact]
		public void FullBufferOverwritesOldestItem() {
			var buffer = new CircularBuffer<int>(3);

			buffer.Enqueue(1);
			buffer.Enqueue(2);
			buffer.Enqueue(3);
			buffer.Enqueue(4);

			Assert.Equal(3, buffer.Count);
			Assert.True(buffer.TryDequeue(out var first));
			Assert.True(buffer.TryDequeue(out var second));
			Assert.True(buffer.TryDequeue(out var third));
			Assert.Equal(2, first);
			Assert.Equal(3, second);
			Assert.Equal(4, third);
		}

		[Fact]
		public void FullBufferCanRejectNewItems() {
			var buffer = new CircularBuffer<int>(2);

			Assert.True(buffer.TryEnqueue(1));
			Assert.True(buffer.TryEnqueue(2));
			Assert.False(buffer.TryEnqueue(3, false));
			Assert.Equal(2, buffer.Count);
		}

		[Fact]
		public void DequeueReleasesReference() {
			var buffer = new CircularBuffer<object>(2);
			var weak = EnqueueDequeueAndGetWeakReference(buffer);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.False(weak.IsAlive);
		}

		[Fact]
		public void ClearReleasesReferences() {
			var buffer = new CircularBuffer<object>(2);
			var weak = EnqueueAndClearAndGetWeakReference(buffer);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.False(weak.IsAlive);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference EnqueueDequeueAndGetWeakReference(CircularBuffer<object> buffer) {
			var value = new object();
			buffer.Enqueue(value);
			Assert.True(buffer.TryDequeue(out var dequeued));
			Assert.Same(value, dequeued);
			return new WeakReference(value);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference EnqueueAndClearAndGetWeakReference(CircularBuffer<object> buffer) {
			var value = new object();
			buffer.Enqueue(value);
			buffer.Clear();
			return new WeakReference(value);
		}
	}
}
