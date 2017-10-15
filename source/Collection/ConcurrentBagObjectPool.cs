using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Open.Collections;

namespace Open.Disposable
{
	public sealed class ConcurrentBagObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class

	{

		public ConcurrentBagObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			Pool = new ConcurrentBag<T>();
		}

		public ConcurrentBagObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}

		ConcurrentBag<T> Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p == null) return false;
			p.Add(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T TryTakeInternal()
		{
			var p = Pool;
			if (p == null) return null;
			p.TryTake(out T item);
			return item;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
		}
		
	}

	public static class ConcurrentBagObjectPool
	{
		public static ConcurrentBagObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ConcurrentBagObjectPool<T>(factory, capacity);
		}

		public static ConcurrentBagObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}
	}
}
