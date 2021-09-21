using System;
using System.Collections.Concurrent;

namespace Open.Disposable
{
	public sealed class ConcurrentQueueObjectPoolSlim<T> : CountTrackedObjectPoolBase<T>
		where T : class
	{

		public ConcurrentQueueObjectPoolSlim(Func<T> factory, Action<T>? recycler, Action<T>? disposer, int capacity = DEFAULT_CAPACITY - 10)
			: base(factory, recycler, disposer, capacity)
		{
			Pool = new();
		}

		public ConcurrentQueueObjectPoolSlim(Func<T> factory, int capacity = DEFAULT_CAPACITY - 10)
			: this(factory, null, null, capacity)
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
			if (p is null) return false;
			p.Enqueue(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T? TryRelease()
		{
			var p = Pool;
			if (p is null) return null;
			p.TryDequeue(out var item);
			return item;
		}

		protected override void OnDispose()
		{
			base.OnDispose();
			Pool = null!;
		}

	}
	public static class ConcurrentQueueObjectPoolSlim
	{
		public static ConcurrentQueueObjectPoolSlim<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ConcurrentQueueObjectPoolSlim<T>(factory, capacity);
		}

		public static ConcurrentQueueObjectPoolSlim<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

		public static ConcurrentQueueObjectPoolSlim<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			 where T : class, IRecyclable
		{
			return new ConcurrentQueueObjectPoolSlim<T>(factory, Recycler.Recycle, null, capacity);
		}

		public static ConcurrentQueueObjectPoolSlim<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return CreateAutoRecycle(() => new T(), capacity);
		}

		public static ConcurrentQueueObjectPoolSlim<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable
		{
			return new ConcurrentQueueObjectPoolSlim<T>(factory, null, d => d.Dispose(), capacity);
		}

		public static ConcurrentQueueObjectPoolSlim<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable, new()
		{
			return CreateAutoDisposal(() => new T(), capacity);
		}

	}
}
