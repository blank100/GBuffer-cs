using System;
using System.Buffers;

namespace Gal.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <para>author gouanlin</para>
    public interface IWriter<T> : IDisposable
    {
        int capacity { get; }
        int length { get; set; }
        int position { get; set; }
        int writableCount { get; }

        /// <summary>
        /// 获取已写入的内容的 Span&lt;T&gt;
        /// </summary>
        Span<T> writtenSpan { get; }

        /// <summary>
        /// 获取已写入的内容的 Memory&lt;T&gt;
        /// </summary>
        Memory<T> writtenMemory { get; }

        Span<T> span { get; }
        Memory<T> memory { get; }

        T[] rawArray { get; }

        T this[int index] { set; }

        IWriter<T> Write(T element);
        IWriter<T> Write(T element1, T element2);
        IWriter<T> Write(T element1, T element2, T element3);

        IWriter<T> Write(ReadOnlySpan<T> elements);
        IWriter<T> Write(ReadOnlyMemory<T> elements);
        IWriter<T> Write(ReadOnlySequence<T> elements);

        /// <summary>
        /// 获取当前位置到 buffer 结束的 Span&lt;T&gt;,如果长度不足 sizeHint ,则扩充 buffer 长度
        /// </summary>
        /// <param name="sizeHint"></param>
        /// <returns></returns>
        Span<T> GetSpan(int sizeHint = 0);

        /// <summary>
        /// 获取当前位置到 buffer 结束的 Memory&lt;T&gt;,如果长度不足 sizeHint ,则扩充 buffer 长度
        /// </summary>
        /// <param name="sizeHint"></param>
        /// <returns></returns>
        Memory<T> GetMemory(int sizeHint = 0);

        /// <summary>
        /// 提示需要指定长度的空间
        /// <para>即从当前位置到 capacity 需要指定长度的空间,不足则扩充 buffer 到足够长度</para>
        /// </summary>
        /// <param name="sizeHint"></param>
        void HintSize(int sizeHint);

        /// <summary>
        /// 指针向后移动指针
        /// </summary>
        /// <param name="count"></param>
        void Advance(int count);

        IWriter<T> Clear();

        /// <summary>
        /// 丢弃当前位置之前的所有数据
        /// </summary>
        /// <returns></returns>
        IWriter<T> Discard();
    }
}