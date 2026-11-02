namespace Gal.Core
{

	public interface IBuffer<T> : IReader<T>, IWriter<IBuffer<T>, T>
	{
		// 消除 IReader<T> 与 IWriter<IBuffer<T>, T> 的同名成员菱形歧义
		new int Length { get; }
		new int Position { get; set; }
		new T this[int index] { get; set; }
	}
}
