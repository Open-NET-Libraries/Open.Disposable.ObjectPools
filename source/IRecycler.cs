using System;
using System.Threading.Tasks;

namespace Open.Disposable
{
	public interface IRecycler<in T> : IDisposable
		where T : class
	{
		/// <summary>
		/// Recycles the item.
		/// </summary>
		bool Recycle(T item);

		/// <summary>
		/// Closes the recycling.  No more should be recycled.
		/// </summary>
		Task Close();
	}
}
