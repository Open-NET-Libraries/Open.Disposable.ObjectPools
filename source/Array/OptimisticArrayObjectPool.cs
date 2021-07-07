/* Based on Roslyn's ObjectPool */

using System;

namespace Open.Disposable
{
	/// <summary>
	/// An extremely fast ObjectPool when the capacity is in the low 100s.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OptimisticArrayObjectPool<T> : InterlockedArrayObjectPool<T>
		where T : class
	{

		public OptimisticArrayObjectPool(Func<T> factory, Action<T>? recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, null /* disposer not applicable here */, capacity)
		{ }

		public OptimisticArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{ }

		// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.
		protected override bool SaveToPocket(T item)
			=> Pocket.SetIfNull(item);

		// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.
		protected override bool Store(T item, int index)
		{
			ref var current = ref Pool.Span[index];
			if (current is not null) return false;
			current = item;
			return true;
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

		public static OptimisticArrayObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			 where T : class, IRecyclable
		{
			return new OptimisticArrayObjectPool<T>(factory, Recycler.Recycle, capacity);
		}

		public static OptimisticArrayObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return CreateAutoRecycle(() => new T(), capacity);
		}

	}
}
