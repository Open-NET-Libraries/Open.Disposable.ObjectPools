using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable
{
	public sealed class ConcurrentQueueObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{

		public ConcurrentQueueObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			Pool = new ConcurrentQueue<T>();
		}

		public ConcurrentQueueObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
			
		}

		ConcurrentQueue<T> Pool;

		public override int Count => Pool?.Count ?? 0;
		
		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p == null) return false;
			p.Enqueue(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T TryTakeInternal()
		{
			var p = Pool;
			if (p == null) return null;
			p.TryDequeue(out T item);
			return item;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
		}

	}

	public static class ConcurrentQueueObjectPool
	{
		public static ConcurrentQueueObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ConcurrentQueueObjectPool<T>(factory, capacity);
		}

		public static ConcurrentQueueObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}
	}
}
