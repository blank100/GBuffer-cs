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
			_buffer = new T[capacity];
			Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enqueue(T item) => TryEnqueue(item, true);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnqueue(T item, bool overwrite = true) {
			if (IsFull) {
				if (!overwrite) return false;
				_tail = (_tail + 1) % Capacity;
				_count--;
			}

			_buffer[_head] = item;
			_head = (_head + 1) % Capacity;
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
			if (IsEmpty) {
				item = default;
				return false;
			}

			item = _buffer[_tail];
			_tail = (_tail + 1) % Capacity;
			_count--;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() {
			_head = 0;
			_tail = 0;
			_count = 0;
		}
	}
}
