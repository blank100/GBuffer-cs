using System.Buffers;

namespace Gal.Core
{
    /// <summary>
    /// Writer 接口：所有写入/清理方法均为 void，不支持链式调用。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public interface IWriter<T> : IDisposable
    {
        int Capacity { get; }
        int Length { get; set; }
        int Position { get; set; }
        int WritableCount { get; }

        /// <summary>
        /// 获取已写入的内容的 Span&lt;T&gt;
        /// </summary>
        Span<T> WrittenSpan { get; }

        /// <summary>
        /// 获取已写入的内容的 Memory&lt;T&gt;
        /// </summary>
        Memory<T> WrittenMemory { get; }

        /// <summary>
        /// 获取当前位置到结尾的 Span&lt;T&gt;
        /// <remarks>此属性应慎用, 在持有返回的 Span&lt;T&gt; 对象期间,如果发生了扩容等修改原始数组的操作,应重新获取 Span&lt;T&gt;</remarks>
        /// </summary>
        Span<T> Span { get; }

        /// <summary>
        /// 获取当前位置到结尾的 Memory&lt;T&gt;
        /// <remarks>此属性应慎用, 在持有返回的 Memory&lt;T&gt; 对象期间,如果发生了扩容等修改原始数组的操作,应重新获取 Memory&lt;T&gt;</remarks>
        /// </summary>
        Memory<T> Memory { get; }

        /// <summary>
        /// 获取原始数组
        /// <remarks>此属性应慎用, 在持有返回的 数组 对象期间,禁止执行会导致原始数组扩容等效果的操作</remarks>
        /// </summary>
        T[] RawArray { get; }

        T this[int index] { set; }

        void Write(T element);
        void Write(T element1, T element2);
        void Write(T element1, T element2, T element3);

        void Write(ReadOnlySpan<T> elements);
        void Write(ReadOnlyMemory<T> elements);
        void Write(ReadOnlySequence<T> elements);

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

        void Clear();

        /// <summary>
        /// 丢弃当前位置之前的所有数据
        /// </summary>
        void Discard();
    }
}
