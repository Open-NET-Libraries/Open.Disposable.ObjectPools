using System;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// An ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
	/// Also does not block when .Give(T item) is called and items are recycled asynchronously.
	/// </summary>
	/// <typeparam name="T">The reference type contained.</typeparam>
	public class RecycleBlockObjectPool<T> : BufferBlockObjectPool<T>
		where T : class
	{
		protected BufferBlock<T> _recycleQueue;
		ActionBlock<T> _recycler;

		/// <summary>
		/// Constructs an ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
		/// Also does not block when .Give(T item) is called and items are recycled asynchronously.
		/// </summary>
		/// <param name="generator">The generator function that creates the items.</param>
		/// <param name="recycler">An recycler function that will process items before making them available.</param>
		/// <param name="maxSize">The maximum size of the object pool.  Default is ushort.MaxValue (65535).</param>
		public RecycleBlockObjectPool(
			Func<T> generator,
			Action<T> recycler,
			int capacity = DEFAULT_CAPACITY) : base(generator, capacity)
		{
			if (recycler != null)
			{
				// By using a buffer block we can 'Recieve/Take' unrecyled items before they get recycled.
				_recycleQueue = new BufferBlock<T>(new DataflowBlockOptions()
				{
					BoundedCapacity = capacity
				});

				_recycleQueue.LinkTo(_recycler = new ActionBlock<T>(item =>
				{
					if (_pool != null) // No need to recycle if we've already been disposed.
					{
						recycler(item);
						_OnRecycled(item);
					}
				},
				new ExecutionDataflowBlockOptions()
				{
					MaxDegreeOfParallelism = 2,
					BoundedCapacity = 2 // Let's not overdo it...
				}));
			}
		}


		public override int Count
			=> (_pool?.Count ?? 0)
			 + (_recycleQueue?.Count ?? 0)
			 + (_recycler?.InputCount ?? 0);

		protected override void OnDispose(bool calledExplicitly)
		{
			base.OnDispose(calledExplicitly);
			_recycleQueue?.Complete();
			_recycleQueue = null;
			_recycler = null; // No need to call complete on the recycler itself... it will finalize all the cleanup...
		}

		protected void _OnRecycled(T item)
		{
			base.GiveInternal(item);
		}

		protected override bool GiveInternal(T item)
		{
			var r = _recycleQueue;
			if (r == null) // No recycler? Defer to default.
				return base.GiveInternal(item); 

			// recycler? check the combined max size first then queue.
			var p = _pool;
			if (p != null && p.Count + r.Count + (_recycler?.InputCount ?? 0) < MaxSize && r.Post(item))
				return true;

			return false;
		}

		public override void TrimTo(int size)
		{
			int count = Count;

			int i = 0; // Prevent an possiblility of infinite loop.
			for (
				var attempts = count - size;
				i < attempts && count > size;
				i++)
			{

				// First try to get any to be recycled items over the trim amount to avoid unnecessary recycling.
				var r = _recycleQueue;
				if (r == null || !r.TryReceive(out T rItem))
				{
					var p = _pool;
					if (p == null || !p.TryReceive(out T item))
						break;
				}

				count = Count;
			}

			if (i != 0) OnTakenFrom(count);

		}


	}

	public static class RecycleBlockObjectPool
	{
		public static RecycleBlockObjectPool<T> Create<T>(Func<T> factory, Action<T> recycler, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new RecycleBlockObjectPool<T>(factory, recycler, capacity);
		}

		public static RecycleBlockObjectPool<T> Create<T>(Action<T> recycler, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), recycler, capacity);
		}
	}
}
