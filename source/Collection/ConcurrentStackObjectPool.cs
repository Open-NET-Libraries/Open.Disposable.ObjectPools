using System;
using System.Collections.Concurrent;

namespace Open.Disposable
{
	public sealed class ConcurrentStackObjectPool<T> : TrimmableCollectionObjectPoolBase<T, ConcurrentStack<T>>
		where T : class
	{

		public ConcurrentStackObjectPool(Func<T> factory, Action<T>? recycler, Action<T>? disposer, int capacity = DEFAULT_CAPACITY)
			: base(new ConcurrentStack<T>(), factory, recycler, disposer, capacity)
		{
		}

		public ConcurrentStackObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, null, capacity)
		{

		}

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p is null) return false;
			p.Push(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T? TryRelease()
		{
			var p = Pool;
			if (p is null) return null;
			p.TryPop(out var item);
			return item;
		}

	}

	public static class ConcurrentStackObjectPool
	{
		public static ConcurrentStackObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ConcurrentStackObjectPool<T>(factory, capacity);
		}

		public static ConcurrentStackObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

		public static ConcurrentStackObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			 where T : class, IRecyclable
		{
			return new ConcurrentStackObjectPool<T>(factory, Recycler.Recycle, null, capacity);
		}

		public static ConcurrentStackObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return CreateAutoRecycle(() => new T(), capacity);
		}

		public static ConcurrentStackObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable
		{
			return new ConcurrentStackObjectPool<T>(factory, null, d => d.Dispose(), capacity);
		}

		public static ConcurrentStackObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable, new()
		{
			return CreateAutoDisposal(() => new T(), capacity);
		}

	}
}
