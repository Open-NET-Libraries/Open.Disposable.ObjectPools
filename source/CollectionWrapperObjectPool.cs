using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.Disposable
{
	public class CollectionWrapperObjectPool<T, TCollection> : TrimmableObjectPoolBase<T>
		where T : class
		where TCollection : class, ICollection<T>
	{
		public CollectionWrapperObjectPool(TCollection pool, Func<T> factory, int capacity = DEFAULT_CAPACITY) : base(factory, capacity)
		{
			Pool = pool;
		}

		protected TCollection Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override bool GiveInternal(T item)
		{
			if (item != null)
			{
				var p = Pool;
				if (p != null && p.Count < MaxSize)
				{
					lock (p) p.Add(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
					return true;
				}
			}

			return false;
		}

		protected override T TryTakeInternal()
		{
			retry:

			var p = Pool;
			var item = p?.FirstOrDefault();
			if (item != null)
			{
				/* Removing the first item is typically horribly inefficient but we can't make assumptions about the implementation here.
				 * It's a trade off between potentially iterating the entire collection before removing the last item, or relying on the underlying implementation.
				 * This implementation is in place for reference more than practice.  Sub classes should override. */

				bool wasRemoved = false;
				lock (p) wasRemoved = p.Remove(item);
				if (!wasRemoved) goto retry;
			}

			return item;
		}		

		protected override void OnDispose(bool calledExplicitly)
		{
			Nullify(ref Pool)?.Clear();
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
