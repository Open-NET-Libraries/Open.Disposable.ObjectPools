using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// An ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
	/// Also does not block when .Give(T item) is called and items are recycled asynchronously.
	/// </summary>
	/// <typeparam name="T">The reference type contained.</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	public class RecyclableObjectPool<T> : SimpleObjectPool<T>
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
		public RecyclableObjectPool(
			Func<T> generator,
			Action<T> recycler,
			ushort maxSize = ushort.MaxValue) : base(generator, maxSize)
		{
			if (recycler != null)
			{
				// By using a buffer block we can 'Recieve/Take' unrecyled items before they get recycled.
				_recycleQueue = new BufferBlock<T>(new DataflowBlockOptions()
				{
					BoundedCapacity = maxSize
				});

				_recycleQueue.LinkTo(_recycler = new ActionBlock<T>(item =>
				{
					if (_pool == null) // No need to recycle if we've already been disposed.
					{
						QueueDisposal(item);
					}
					else
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

		/// <summary>
		/// Total number of items in the pool.
		/// </summary>
		public override int Count => (_pool?.Count ?? 0) + (_recycleQueue?.Count ?? 0) + (_recycler?.InputCount ?? 0);

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
			if (item != null)
			{
				var r = _recycleQueue;
				if (r == null) // No recycler? Defer to default.
					return base.GiveInternal(item); 

				// recycler? check the combined max size first then queue.
				var p = _pool;
				if (p != null && p.Count + r.Count + (_recycler?.InputCount ?? 0) < _maxSize && r.Post(item))
					return true;

				QueueDisposal(item);
			}

			return false;
		}

	}
}
