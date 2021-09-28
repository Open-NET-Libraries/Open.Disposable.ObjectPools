using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Disposable
{
	public class CollectionWrapperObjectPool<T, TCollection> : TrimmableGenericCollectionObjectPoolBase<T, TCollection>
		where T : class
		where TCollection : class, ICollection<T>
	{
		public CollectionWrapperObjectPool(TCollection pool, Func<T> factory, Action<T>? recycler, Action<T>? disposer, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: base(pool, factory, recycler, disposer, capacity, countTrackingEnabled)
		{
		}

		public CollectionWrapperObjectPool(TCollection pool, Func<T> factory, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: this(pool, factory, null, null, capacity, countTrackingEnabled)
		{
		}

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p is not null)
			{
				lock (p)
				{
					// It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
					// The lock operation should be quick enough to not pile up too many items.
					p.Add(item);
					return true;
				}
			}

			return false;
		}

		protected override T? TryRelease()
		{
		retry:

			var p = Pool;
			var item = p?.FirstOrDefault();
			if (item is null) return null;
			/* Removing the first item is typically horribly inefficient but we can't make assumptions about the implementation here.
				 * It's a trade off between potentially iterating the entire collection before removing the last item, or relying on the underlying implementation.
				 * This implementation is in place for reference more than practice.  Sub classes should override. */

			bool wasRemoved;
			lock (p!) wasRemoved = p.Remove(item);
			if (!wasRemoved) goto retry;

			return item;
		}

	}

	public class CollectionWrapperObjectPool<T> : CollectionWrapperObjectPool<T, ICollection<T>>
		where T : class
	{
		public CollectionWrapperObjectPool(ICollection<T> pool, Func<T> factory, int capacity = DEFAULT_CAPACITY) : base(pool, factory, capacity)
		{
		}
	}
}
