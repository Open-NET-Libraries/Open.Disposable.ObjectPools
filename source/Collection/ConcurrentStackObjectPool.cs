using System;
using System.Collections.Concurrent;

namespace Open.Disposable
{
	public sealed class ConcurrentStackObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{

		public ConcurrentStackObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			: base(factory, recycler, capacity, countTrackingEnabled)
		{
			Pool = new ConcurrentStack<T>();
		}

		public ConcurrentStackObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			: this(factory, null, capacity, countTrackingEnabled)
		{

		}

		ConcurrentStack<T> Pool;

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p == null) return false;
			p.Push(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			return true;
		}

		protected override T TryRelease()
		{
			var p = Pool;
			if (p == null) return null;
			p.TryPop(out T item);
			return item;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
		}

	}

	public static class ConcurrentStackObjectPool
	{
		public static ConcurrentStackObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class
		{
			return new ConcurrentStackObjectPool<T>(factory, capacity, countTrackingEnabled);
		}

		public static ConcurrentStackObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, new()
		{
			return Create(() => new T(), capacity, countTrackingEnabled);
		}

		public static ConcurrentStackObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, IRecyclable
		{
			Action<T> recycler = null;
			if (autoRecycle) recycler = Recycler.Recycle;
			return new ConcurrentStackObjectPool<T>(factory, recycler, capacity, countTrackingEnabled);
		}

		public static ConcurrentStackObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY, bool countTrackingEnabled = false)
			where T : class, IRecyclable, new()
		{
			return Create(() => new T(), autoRecycle, capacity, countTrackingEnabled);
		}

	}
}
