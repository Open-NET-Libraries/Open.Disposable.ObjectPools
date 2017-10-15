using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// An ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
	/// </summary>
	/// <typeparam name="T">The reference type contained.</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	public class BufferBlockObjectPool<T> : TrimmableObjectPoolBase<T>
		where T : class
	{
		/// <summary>
		/// Constructs an ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
		/// </summary>
		/// <param name="factory">The generator function that creates the items.</param>
		/// <param name="recycler">The optional function that operates on an item just before entering the pool.</param>
		/// <param name="maxSize">The maximum size of the object pool.  Default is ushort.MaxValue (65535).</param>
		public BufferBlockObjectPool(
			Func<T> factory,
			Action<T> recycler,
			int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			_pool = new BufferBlock<T>(new DataflowBlockOptions()
			{
				BoundedCapacity = capacity
			});
		}

		public BufferBlockObjectPool(
			Func<T> factory,
			int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}

		protected BufferBlock<T> _pool;

		Task<bool> Generate(out Task<T> actual)
		{
			actual = Task.Run(Factory);
			return actual.ContinueWith(t =>
				t.Status == TaskStatus.RanToCompletion && (_pool?.Post(t.Result) ?? false) // .Post is synchronous but should return immediately if bounded capacity is met.
			);
		}

		public override int Count => _pool?.Count ?? 0;

		protected override sealed T TryTakeInternal()
		{
			var p = _pool;
			if (p == null) return null;
			p.TryReceive(out T item);
			return item;
		}

		protected override async Task<T> TakeAsyncInternal()
		{
			CancellationTokenSource ts = null; // If something goes wrong we need a way to cancel.
			var taken = _pool?.ReceiveAsync((ts = new CancellationTokenSource()).Token);
			if (taken != null)
			{
				while (taken.Status != TaskStatus.RanToCompletion)
				{
					// Ok we need to push into the pool...
					var generated = Generate(out Task<T> actual);

					if (await Task.WhenAny(taken, generated) == generated && !generated.Result)
					{
						// ^^^ Not received yet and was not added to pool? Uh-oh...
						ts.Cancel(); // Since the generator failed or was unable to be added, then cancel waiting to recieve it.

						// Was it actually cancelled? 
						if (await taken.ContinueWith(t => t.IsCanceled)) // || t.IsFaulted ... .ReceiveAsync should never fault.
						{
							if (actual.IsFaulted)
								throw actual.Exception; // Possible generator failure.
							if (actual.Status == TaskStatus.RanToCompletion)
								return actual.Result; // Don't let it go to waste.

							// This is a rare edge case where there's no fault but did not complete.  Effectively erroneous.
							Debug.Fail("Somehow the generate task did not complete and had no fault.");
							return base.Take();
						}
					}
				}

				OnTakenFrom();
				return taken.Result;
			}

			return base.Take(); // Pool is closed/completed.  Just do this here without queueing.
		}

		public override sealed T Take()
		{
			// See if there's one available already.
			if (TryTake(out T firstTry))
				return firstTry;

			if (_pool == null)
				return Factory();

			return TakeAsync().Result;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			Nullify(ref _pool)?.Complete();
		}

		protected override bool Receive(T item)
		{
			return _pool.Post(item);
		}

		protected override Task<bool> GiveInternalAsync(T item)
		{
			return _pool.SendAsync(item);
		}

	}

	public static class BufferBlockObjectPool
	{
		public static BufferBlockObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new BufferBlockObjectPool<T>(factory, capacity);
		}

		public static BufferBlockObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}
	}
}
