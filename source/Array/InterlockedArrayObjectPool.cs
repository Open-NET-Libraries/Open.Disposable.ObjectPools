/* Based on Roslyn's ObjectPool */

using System;
using System.Linq;
using System.Threading;

namespace Open.Disposable
{
	/// <summary>
	/// An extremely fast ObjectPool when the capacity is in the low 100s.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class InterlockedArrayObjectPool<T> : ObjectPoolBase<T>
		where T : class
	{

		public InterlockedArrayObjectPool(Func<T> factory, Action<T>? recycler, Action<T>? disposer, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, disposer, capacity)
		{
			Pool = new T[capacity - 1];
		}

		public InterlockedArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, null, capacity)
		{ }

		protected Memory<T> Pool;

		// Sets a limit on what has been stored yet to prevent over searching the array unnecessarily.. 
		protected int MaxStored;
		protected const int MaxStoredIncrement = 5; // Instead of every one.

		public override int Count
		{
			get
			{
				var count = PocketCount;
				foreach (var e in Pool.Span)
					if (e != null) ++count;
				return count;
			}
		}

		protected virtual bool Store(T item, int index)
		{
			ref var current = ref Pool.Span[index];
			return current is null && null == Interlocked.CompareExchange(ref current, item, null);
		}

		protected T? Retrieve(int index)
		{
			ref var current = ref Pool.Span[index];
			return current is not null && current == Interlocked.CompareExchange(ref current, default, current)
				? current : null;
		}

		protected override bool Receive(T item)
		{
			var len = Pool.Length;

			for (var i = 0; i < len; i++)
			{
				if (Store(item, i))
				{
					var m = MaxStored;
					if (i >= m) Interlocked.CompareExchange(ref MaxStored, m + MaxStoredIncrement, m);

					return true;
				}
			}

			return false;
		}

		protected override T? TryRelease()
		{
			var len = Pool.Length;
			for (var i = 0; i < MaxStored && i < len; i++)
			{
				var item = Retrieve(i);
				if (item != null) return item;
			}

			return null;
		}

		protected override void OnDispose()
		{
			base.OnDispose();
			Pool = Memory<T>.Empty;
			MaxStored = 0;
		}

	}

	public static class InterlockedArrayObjectPool
	{
		public static InterlockedArrayObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new InterlockedArrayObjectPool<T>(factory, capacity);
		}

		public static InterlockedArrayObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

		public static InterlockedArrayObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			 where T : class, IRecyclable
		{
			return new InterlockedArrayObjectPool<T>(factory, Recycler.Recycle, null, capacity);
		}

		public static InterlockedArrayObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return CreateAutoRecycle(() => new T(), capacity);
		}

		public static InterlockedArrayObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable
		{
			return new InterlockedArrayObjectPool<T>(factory, null, d => d.Dispose(), capacity);
		}

		public static InterlockedArrayObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IDisposable, new()
		{
			return CreateAutoDisposal(() => new T(), capacity);
		}

	}
}
