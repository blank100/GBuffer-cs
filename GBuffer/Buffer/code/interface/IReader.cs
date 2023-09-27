using System;

namespace Gal.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <para>author gouanlin</para>
    public interface IReader<T> : IDisposable
    {
        int length { get; }
        int position { get; set; }

        /// <summary>
        /// 剩余可读取长度
        /// </summary>
        int readableCount { get; }

        ReadOnlyMemory<T> memory { get; }
        ReadOnlySpan<T> span { get; }

        T this[int index] { get; }

        T Read();

        ReadOnlySpan<T> GetSpan(int count);
        ReadOnlyMemory<T> GetMemory(int count);

        /// <summary>
        /// 指针向后移动指针
        /// </summary>
        /// <param name="count"></param>
        void Advance(int count);
    }
}