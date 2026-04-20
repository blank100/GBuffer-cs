namespace Gal.Core
{

	public interface IBuffer2<out TSelf, T> : IReader<T>, IWriter2<TSelf,T> where TSelf : IWriter2<TSelf,T>
	{
	}
}
