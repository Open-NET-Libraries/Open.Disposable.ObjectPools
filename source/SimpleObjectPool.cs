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
	public class SimpleObjectPool<T> : DisposableBase
		where T : class
	{
		protected BufferBlock<T> _pool;
		protected Func<T> _generator;

		// micro-optimization for retrieving this value as read-only is slightly slower.
		protected ushort _maxSize;

		/// <summary>
		/// Constructs an ObjectPool that when .Take() is called will return the first possible item even if one is returned to the pool before the generator function completes.
		/// </summary>
		/// <param name="generator">The generator function that creates the items.</param>
		/// <param name="maxSize">The maximum size of the object pool.  Default is ushort.MaxValue (65535).</param>
		public SimpleObjectPool(
			Func<T> generator,
			ushort maxSize = ushort.MaxValue)
		{
			MaxSize = _maxSize = maxSize;
			_generator = generator;
			_pool = new BufferBlock<T>(new DataflowBlockOptions()
			{
				BoundedCapacity = maxSize
			});
		}

		/// <summary>
		/// Defines the maximum at which trimming should allow.
		/// </summary>
		public readonly ushort MaxSize;

		Task<bool> Generate(out Task<T> actual)
		{
			actual = Task.Run(_generator);
			return actual.ContinueWith(t =>
				t.Status == TaskStatus.RanToCompletion && _pool.Post(t.Result) // .Post is synchronous but should return immediately if bounded capacity is met.
			);
		}

		/// <summary>
		/// Total number of items in the pool.
		/// </summary>
		public virtual int Count => _pool?.Count ?? 0;

		protected virtual void _onTaken()
		{

		}

		/// <summary>
		/// Attempts to extract an item from the pool but if none are available returns false.
		/// </summary>
		/// <param name="value">The item taken.</param>
		/// <returns>If one was available.</returns>
		public bool TryTake(out T value)
		{
			value = null;
			bool taken = _pool?.TryReceive(out value) ?? false;
			if (taken) _onTaken();
			return taken;
		}

		/// <summary>
		/// Awaits an available item from the pool.  If none are available it generates one.
		/// </summary>
		/// <returns>An item removed from the pool or generated.</returns>
		public async Task<T> TakeAsync()
		{
			while (true)
			{
				// See if there's one available already.
				if (TryTake(out T firstTry))
					return firstTry;

				// Setup the tasks.  One will win the race.
				var p = _pool;
				if (p == null) break; // Disposed pool, no need to do anything fancy.  Just (break=>) generate and return the result.
				var isAvailable = p.OutputAvailableAsync();
				var generated = Generate(out Task<T> actual);

				// Allow for re-entrance here.
				await Task.WhenAny(isAvailable, generated);

				// Did something get added and is waiting for us? Check...
				if (TryTake(out T secondTry))
					return secondTry;

				// Ok so now wait for the generated task to be complete and check its result.
				// If one was added, retry the loop.
				if (!await generated) // Was not added to pool? Uh-oh...
				{
					if (actual.IsFaulted)
						throw actual.Exception;
					if (actual.Status == TaskStatus.RanToCompletion)
						return actual.Result; // Don't let it go to waste.

					// This is a rare edge case where there's no fault but did not complete.  Effectively erroneous.
					Debug.Fail("Somehow the generate task did not complete and had no fault.");
					break; // No more can be generated? Then time to go...
				}
			}

			return _generator(); // Pool is closed/completed.  Just do this here without queueing.
		}

		/// <summary>
		/// Awaits an available item from the pool.  If none are available it generates one.
		/// </summary>
		/// <returns>An item removed from the pool or generated.</returns>
		public T Take()
		{
			return TakeAsync().Result;
		}

		protected static void ClearAndDisposeContents(BufferBlock<T> pool)
		{
			pool.Complete(); // No more... You're done...
			if (pool.TryReceiveAll(out IList<T> items))
			{
				foreach (var i in items)
					QueueDisposal(i);
			}
		}

		protected static void ClearAndDisposeContents(ref BufferBlock<T> pool)
		{
			ClearAndDisposeContents(Interlocked.Exchange(ref pool, null));
		}

		/// <summary>
		/// Will clear out the pool.
		/// </summary>
		public void Clear()
		{
			foreach (var i in Dump())
				QueueDisposal(i);
		}

		protected virtual void OnDumping()
		{

		}

		/// <summary>
		/// Replaces current bag with a new one and returns the existing bag without disposing the contents.
		/// Will not include incomming items currently being returned to the pool.
		/// </summary>
		/// <returns>The previous ConcurrentBag<T>.</returns>
		public IList<T> Dump()
		{
			AssertIsAlive();
			OnDumping();
			var p = Interlocked.Exchange(ref _pool, new BufferBlock<T>());
			if (IsDisposed) // It is possible that a dispose was called immediately after exchanging the current pool.  In that case, ensure the pool is nullified and disposed.
				ClearAndDisposeContents(ref _pool);

			return p.TryReceiveAll(out IList<T> items) ? items : new List<T>();
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			_generator = null;
			ClearAndDisposeContents(ref _pool);
		}

		/// <summary>
		/// Receives an item and adds it to the pool. Ignores null references.
		/// WARNING: The item is considered 'dead' but resurrectable so be sure not to hold on to the item's reference.
		/// </summary>
		/// <param name="item">The item to give up to the pool.</param>
		public void Give(T item)
		{
			GiveInternal(item);
		}

		protected virtual bool GiveInternal(T item)
		{
			if (item != null)
			{
				if (_pool?.Post(item) ?? false) return true;

				QueueDisposal(item);
			}
			return false;
		}

		protected static void QueueDisposal(T item)
		{
			(item as IDisposable)?.QueueForDisposal();
		}

		/// <summary>
		/// Receives items and iteratively adds them to the pool.
		/// WARNING: These items are considered 'dead' but resurrectable so be sure not to hold on to their reference.
		/// </summary>
		/// <param name="items">The items to give up to the pool.</param>
		public void Give(IEnumerable<T> items)
		{
			if (items != null)
				foreach (var i in items)
					Give(i);
		}

		/// <summary>
		/// Receives items and iteratively adds them to the pool.
		/// WARNING: These items are considered 'dead' but resurrectable so be sure not to hold on to their reference.
		/// </summary>
		/// <param name="item2">The first item to give up to the pool.</param>
		/// <param name="item2">The second item to give up to the pool.</param>
		/// <param name="items">The remaining items to give up to the pool.</param>
		public void Give(T item1, T item2, params T[] items)
		{
			Give(item1);
			Give(item2);
			Give(items);
		}
	}
}
