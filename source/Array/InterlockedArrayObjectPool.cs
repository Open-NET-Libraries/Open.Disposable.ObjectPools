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
	public class InterlockedArrayObjectPool<T> : ObjectPoolBase<T>
		where T : class
	{

		public InterlockedArrayObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			AllowPocket = true;
		}

		public InterlockedArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}

		Element[] Pool;

		[DebuggerDisplay("{Value,nq}")]
		protected struct Element
		{
			T _value;
			internal bool Save(ref T value)
			{
				if (_value != null) return false;
				value = Interlocked.Exchange(ref _value, value);
				return value == null;
			}

			internal T TryRetrieve()
			{
				var item = _value;
				if (item != null)
				{
					if (item == Interlocked.CompareExchange(ref _value, null, item))
					{
						return item;
					}
				}
				return null;
			}
		}

		protected override bool Receive(T item)
		{
			var elements = Pool;
			var len = elements?.Length ?? 0;

			for (int i = 0; i < len; i++)
			{
				if (elements[i].Save(ref item))
					return true;
			}

			return false;
		}

		protected override T TryTakeInternal()
		{
			// We missed getting the first item or it wasn't there.
			var elements = Pool;
			var len = elements?.Length ?? 0;

			for (int i = 0; i < len; i++)
			{
				var item = elements[i].TryRetrieve();
				if (item != null) return item;
			}

			return null;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
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
	}
}
