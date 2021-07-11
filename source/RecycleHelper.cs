using System;

namespace Open.Disposable
{
	public struct RecycleHelper<T> : IDisposable
		where T : class
	{
		private IObjectPool<T>? _pool;

		private RecycleHelper(IObjectPool<T> pool, T item)
		{
			_pool = pool;
			_item = item;
		}

		public RecycleHelper(IObjectPool<T> pool)
			: this(pool ?? throw new ArgumentNullException(nameof(pool)), pool.Take())
		{

		}

		private T? _item;
		public T Item => _item ?? throw new ObjectDisposedException(GetType().ToString());

		public void Dispose()
		{
			var i = Item;
			_item = null;
			var p = _pool ?? throw new ObjectDisposedException(GetType().ToString());
			_pool = null;
			p.Give(i);
		}
	}
}
