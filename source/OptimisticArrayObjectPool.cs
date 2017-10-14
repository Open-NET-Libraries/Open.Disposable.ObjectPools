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
		public OptimisticArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY) : base(factory, capacity)
		{
			_pool = new Element[capacity - 1];
		}

		[DebuggerDisplay("{Value,nq}")]
		private struct Element
		{
			internal T Value;
		}

		Element[] _pool;
		T _firstItem;

		protected override bool GiveInternal(T item)
		{
			if (item == null) return false;

			// First see if optimisically we can store in _firstItem;
			if (_firstItem == null)
			{
				_firstItem = item;
				return true;
			}
			// Else iterate to find an empty slot.
			else
			{
				var elements = _pool;
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
			var elements = _pool;
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
			_firstItem = null;
			var pool = Nullify(ref _pool);
			var len = pool?.Length ?? 0;

			for (var i = 0; i < len; i++)
				pool[i].Value = null;
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
