/* Based on Roslyn's ObjectPool */

using System;
using System.Diagnostics;
using System.Threading;

namespace Open.Disposable
{
	/// <summary>
	/// An extremely fast ObjectPool when the capacity is in the low 100s.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OptimisticArrayObjectPool<T> : ObjectPoolBase<T>
		where T : class
	{

		public OptimisticArrayObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			AllowPocket = false;
			Pool = new Element[capacity - 1];
		}

		public OptimisticArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}


		[DebuggerDisplay("{Value,nq}")]
		protected struct Element
		{
			internal T Value;
		}

		T _firstItem;
		Element[] Pool;

		protected override bool Receive(T item)
		{
			// First see if optimisically we can store in _firstItem;
			if (_firstItem == null)
			{
				_firstItem = item;
				return true;
			}
			// Else iterate to find an empty slot.
			else
			{
				var elements = Pool;
				var len = elements?.Length ?? 0;

				for (int i = 0; i < len; i++)
				{
					var e = elements[i];
					// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.
					if (e.Value == null)
					{
						e.Value = item;
						return true;
					}
				}

				return false;
			}
		}

		protected override T TryTakeInternal()
		{
			T item = _firstItem;

			// First check and see if we actually were able to get the first item.
			if (item != null && item == Interlocked.CompareExchange(ref _firstItem, null, item))
				return item;

			// We missed getting the first item or it wasn't there.
			var elements = Pool;
			var len = elements?.Length ?? 0;

			for (int i = 0; i < len; i++)
			{
				var e = elements[i];
				item = e.Value;
				if (item != null)
				{
					if (item == Interlocked.CompareExchange(ref e.Value, null, item))
					{
						return item;
					}
				}
			}

			return null;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
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
	}
}
