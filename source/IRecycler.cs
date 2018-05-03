using System;
using System.Threading.Tasks;

namespace Open.Disposable
{
	interface IRecycler<T> : IDisposable
		where T : class
	{
		bool Recycle(T item);
		Task Close();
	}
}
