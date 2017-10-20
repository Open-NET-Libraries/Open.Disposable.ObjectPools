using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable
{
	public sealed class StackObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{

		public StackObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			Pool = new Stack<T>(capacity); // Very very slight speed improvment when capacity is set.
		}

		public StackObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
			
		}

		Stack<T> Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override bool Receive(T item)
		{
			var p = Pool;
			if (p!=null)
			{
				// It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
				// The lock operation should be quick enough to not pile up too many items.
				lock (p) p.Push(item);
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
						return p.Pop();
				}

			}

			return null;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool = null;
		}
	}

	public static class StackObjectPool
    {
		public static StackObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new StackObjectPool<T>(factory, capacity);
		}

		public static StackObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

        public static StackObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
             where T : class, IRecyclable
        {
            Action<T> recycler = null;
            if (autoRecycle) recycler = Recycler.Recycle;
            return new StackObjectPool<T>(factory, recycler, capacity);
        }

        public static StackObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
            where T : class, IRecyclable, new()
        {
            return Create(() => new T(), autoRecycle, capacity);
        }

    }
}
