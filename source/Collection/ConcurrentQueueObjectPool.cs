using System;
using System.Collections.Concurrent;

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

		// For ConcurrentQueue disable using the Pocket.  It's fast enough as it is...
		protected override bool SaveToPocket(T item)
		{
			return false;
		}

		protected override T TakeFromPocket()
		{
			return null;
		}

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p == null) return false;
			p.Enqueue(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T TryRelease()
		{
			var p = Pool;
			if (p == null) return null;
			p.TryDequeue(out T item);
			return item;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			if(calledExplicitly)
			{
				Pool = null;
			}
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

		public static ConcurrentQueueObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable
		{
			Action<T> recycler = null;
			if (autoRecycle) recycler = Recycler.Recycle;
			return new ConcurrentQueueObjectPool<T>(factory, recycler, capacity);
		}

		public static ConcurrentQueueObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return Create(() => new T(), autoRecycle, capacity);
		}
	}
}
