using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gal.Core
{
    /// <summary>
    /// Reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <author>gouanlin</author>
    public class Reader<T> : IReader<T>
    {
        private readonly T[] _buffer;
        private int _position;

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        public int Position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Debug.Assert(0 <= value && value <= _buffer.Length, $"{nameof(Position)} cannot be less than 0 or greater than {nameof(Length)}");
                _position = value;
            }
        }

        public int ReadableCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length - _position;
        }

        public ReadOnlyMemory<T> Memory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.AsMemory(_position);
        }

        public ReadOnlySpan<T> Span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _buffer[_position..];
        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer[index];
        }

        public Reader(T[] original) {
            _buffer = original;
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read() => _buffer[_position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) {
            Debug.Assert(_position + count >= 0, "移动后的指针位置不能未负数");
            Debug.Assert(_position + count <= _buffer.Length, "移动后的指针位置超出了buffer的容量");
            _position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> GetSpan(int count) {
            Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
            Debug.Assert(count <= _buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
            return _buffer[_position..(_position + count)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> GetMemory(int count) {
            Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
            Debug.Assert(count <= _buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
            return _buffer[_position..(_position + count)];
        }

        public void Dispose() { }
    }
}
