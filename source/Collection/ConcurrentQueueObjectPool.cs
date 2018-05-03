using System;
using System.Collections.Concurrent;

namespace Open.Disposable
{
	public sealed class ConcurrentQueueObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{

		public ConcurrentQueueObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			: base(factory, recycler, capacity, countTrackingEnabled)
		{
			Pool = new ConcurrentQueue<T>();
		}

		public ConcurrentQueueObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			: this(factory, null, capacity, countTrackingEnabled)
		{

		}

		ConcurrentQueue<T> Pool;

		/*
         * NOTE: ConcurrentQueue is very fast and will perform quite well without using the 'Pocket' feature.
         * Benchmarking reveals that mixed read/writes (what really matters) are still faster with the pocket enabled so best to keep it so.
         */

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
			if (calledExplicitly)
			{
				Pool = null;
			}
		}

	}

	public static class ConcurrentQueueObjectPool
	{
		public static ConcurrentQueueObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class
		{
			return new ConcurrentQueueObjectPool<T>(factory, capacity, countTrackingEnabled);
		}


		public static ConcurrentQueueObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, new()
		{
			return Create(() => new T(), capacity, countTrackingEnabled);
		}

		public static ConcurrentQueueObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, IRecyclable
		{
			Action<T> recycler = null;
			if (autoRecycle) recycler = Recycler.Recycle;
			return new ConcurrentQueueObjectPool<T>(factory, recycler, capacity, countTrackingEnabled);
		}

		public static ConcurrentQueueObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, IRecyclable, new()
		{
			return Create(() => new T(), autoRecycle, capacity, countTrackingEnabled);
		}
	}
}
