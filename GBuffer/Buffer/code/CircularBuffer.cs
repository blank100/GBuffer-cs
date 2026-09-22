namespace Gal.Core
{
	/// <summary>
	/// 环形缓冲区
	/// </summary>
    /// <author>gouanlin</author>
	public sealed class CircularBuffer<T>
	{
		private readonly T[] _buffer;
		private int _head;
		private int _tail;
		private int _count;

		public int Capacity {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _buffer.Length;
		}
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count;
		}
		public bool IsFull {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count == Capacity;
		}
		public bool IsEmpty {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CircularBuffer(int capacity) {
			if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

			_buffer = new T[capacity];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enqueue(T item) => TryEnqueue(item, true);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnqueue(T item, bool overwrite = true) {
			var capacity = _buffer.Length;
			if (_count == capacity) {
				if (!overwrite) return false;
				_tail = NextIndex(_tail, capacity);
				_count--;
			}

			_buffer[_head] = item;
			_head = NextIndex(_head, capacity);
			_count++;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Dequeue() {
			if (!TryDequeue(out var t)) throw new InvalidOperationException("buffer is empty.");
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryDequeue(out T item) {
			if (_count == 0) {
				item = default;
				return false;
			}

			var tail = _tail;
			item = _buffer[tail];
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) _buffer[tail] = default;

			_tail = NextIndex(tail, _buffer.Length);
			_count--;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() {
			_head = 0;
			_tail = 0;
			_count = 0;
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Array.Clear(_buffer, 0, _buffer.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int NextIndex(int index, int capacity) {
			index++;
			return index == capacity ? 0 : index;
		}
	}
}
