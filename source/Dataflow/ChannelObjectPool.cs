using System;
using System.Diagnostics;
using System.IO.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Disposable
{
	/// <summary>
	/// An ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
	/// </summary>
	/// <typeparam name="T">The reference type contained.</typeparam>
	public class ChannelObjectPool<T> : ObjectPoolBase<T>
		where T : class
	{
		/// <summary>
		/// Constructs an ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
		/// </summary>
		/// <param name="factory">The generator function that creates the items.</param>
		/// <param name="recycler">The optional function that operates on an item just before entering the pool.</param>
		/// <param name="maxSize">The maximum size of the object pool.  Default is ushort.MaxValue (65535).</param>
		public ChannelObjectPool(
			Func<T> factory,
			Action<T> recycler,
			int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			_pool = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
			{
				FullMode = BoundedChannelFullMode.DropWrite
			});
		}

		public ChannelObjectPool(
			Func<T> factory,
			int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}

		protected Channel<T> _pool;

		Task<bool> Generate(out Task<T> actual)
		{
			actual = Task.Run(Factory);
			return actual.ContinueWith(t =>
				t.Status == TaskStatus.RanToCompletion && (_pool?.Writer.TryWrite(t.Result) ?? false) // .Post is synchronous but should return immediately if bounded capacity is met.
			);
		}

		protected override sealed T TryTakeInternal()
		{
			var p = _pool;
			if (p == null) return null;
			p.Reader.TryRead(out T item);
			return item;
		}

		protected override async Task<T> TakeAsyncInternal()
		{
			CancellationTokenSource ts = null; // If something goes wrong we need a way to cancel.
			
			retry:
			var available = _pool?.Reader.WaitToReadAsync(ts.Token);
			if (available != null)
			{
				while (available.Status != TaskStatus.RanToCompletion)
				{
				
					// Ok we need to push into the pool...
					var generated = Generate(out Task<T> actual);

					if (await Task.WhenAny(available, generated) == generated && !generated.Result)
					{
						// ^^^ Not received yet and was not added to pool? Uh-oh...
						ts.Cancel(); // Since the generator failed or was unable to be added, then cancel waiting to recieve it.

						// Was it actually cancelled? 
						if (await available.ContinueWith(t => t.IsCanceled)) // || t.IsFaulted ... .ReceiveAsync should never fault.
						{
							if (actual.IsFaulted)
								throw actual.Exception; // Possible generator failure.
							if (actual.Status == TaskStatus.RanToCompletion)
								return actual.Result; // Don't let it go to waste.

							// This is a rare edge case where there's no fault but did not complete.  Effectively erroneous.
							Debug.Fail("Somehow the generate task did not complete and had no fault.");
							return Take();
						}
					}

					// If generated was successful but available was not, then we need to try again (loop)...
				}

				if (available.Result)
				{
					var p = _pool;
					if (p != null && p.Reader.TryRead(out T item))
					{
						OnTakenFrom();
						return item;
					}
					else
					{
						goto retry;
					}
				}

				
			}

			return base.Take(); // Pool is closed/completed.  Just do this here without queueing.
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Nullify(ref _pool)?.Writer.Complete();
		}

		protected override bool Receive(T item)
		{
			return _pool.Writer.TryWrite(item);
		}

	}

	public static class ChannelObjectPool
	{
		public static ChannelObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ChannelObjectPool<T>(factory, capacity);
		}

		public static ChannelObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}
	}
}
