/* Based on Roslyn's ObjectPool */

using System;
using System.Threading;

namespace Open.Disposable
{
	/// <summary>
	/// An extremely fast ObjectPool when the capacity is in the low 100s.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OptimisticArrayObjectPool<T> : InterlockedArrayObjectPool<T>
		where T : class
	{

		public OptimisticArrayObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{ }

		public OptimisticArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{ }

		// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.
		protected override bool SaveToPocket(T item)
		{
			return Pocket.SetIfNull(item);
		}

		protected override bool Receive(T item)
		{
			var elements = Pool;
			var len = elements?.Length ?? 0;

			for (int i = 0; i < len; i++)
			{
				// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.
				if (elements[i].SetIfNull(item))
				{
					var m = MaxStored;
					if (i >= m) Interlocked.CompareExchange(ref MaxStored, m + MaxStoredIncrement, m);

					return true;
				}
			}

			return false;
		}

	}

	public static class OptimisticArrayObjectPool
	{
		public static OptimisticArrayObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new OptimisticArrayObjectPool<T>(factory, capacity);
		}

		public static OptimisticArrayObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

		public static OptimisticArrayObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			 where T : class, IRecyclable
		{
			Action<T> recycler = null;
			if (autoRecycle) recycler = Recycler.Recycle;
			return new OptimisticArrayObjectPool<T>(factory, recycler, capacity);
		}

		public static OptimisticArrayObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return Create(() => new T(), autoRecycle, capacity);
		}

	}
}
