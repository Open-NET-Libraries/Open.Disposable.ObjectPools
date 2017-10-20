using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable
{
	public sealed class QueueObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{

		public QueueObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			Pool = new Queue<T>(capacity); // Very very slight speed improvment when capacity is set.
		}

		public QueueObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
			
		}

		Queue<T> Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p!=null)
			{
				// It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
				// The lock operation should be quick enough to not pile up too many items.
				lock (p) p.Enqueue(item);
				return true;
			}

			return false;
		}

		protected override T TryRelease()
		{
			var p = Pool;
			if (p!=null && p.Count != 0)
			{
				lock (p)
				{
					if (p.Count!=0)
						return p.Dequeue();
				}

			}

			return null;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
		}
	}

	public static class QueueObjectPool
	{
		public static QueueObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new QueueObjectPool<T>(factory, capacity);
		}

		public static QueueObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

        public static QueueObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
            where T : class, IRecyclable
        {
            Action<T> recycler = null;
            if (autoRecycle) recycler = Recycler.Recycle;
            return new QueueObjectPool<T>(factory, recycler, capacity);
        }

        public static QueueObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
            where T : class, IRecyclable, new()
        {
            return Create(() => new T(), autoRecycle, capacity);
        }

    }
}
