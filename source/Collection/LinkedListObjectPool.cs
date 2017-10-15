using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable
{
	public class LinkedListObjectPool<T> : CollectionWrapperObjectPool<T, LinkedList<T>>
		where T : class
	{
		public LinkedListObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(new LinkedList<T>(), factory, recycler, capacity)
		{
		}

		public LinkedListObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}


		protected override T TryTakeInternal()
		{
			var p = Pool;
			if (p == null) return null;
			T item;
			lock(p)
			{
				var node = p.Last;
				if (node == null) return null;
				item = node.Value;
				p.RemoveLast();
			}

			return item;
		}

	}

	public static class LinkedListObjectPool
	{
		public static LinkedListObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new LinkedListObjectPool<T>(factory, capacity);
		}

		public static LinkedListObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}
	}
}
