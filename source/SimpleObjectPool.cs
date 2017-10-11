using Open.Collections;
using Open.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Disposable
{
	[DebuggerDisplay("Count = {count}")]
	public class SimpleObjectPool<T> : DisposableBase
		where T : class
	{

		protected ConcurrentBag<T> _pool; // Not readonly because pools can be 'dumped' or swapped out.
		protected Func<T> _generator;
		protected Action<T> _recycler;

		public SimpleObjectPool(
			ushort maxSize,
			Func<T> generator,
			Action<T> recycler = null)
		{
			_maxSize = maxSize;

			_generator = generator;
			_recycler = recycler;

			_pool = new ConcurrentBag<T>();
		}

		protected ushort _maxSize;
		/// <summary>
		/// Defines the maximum at which trimming should allow.
		/// </summary>
		public ushort MaxSize => _maxSize;


		/// <summary>
		/// Current number of objects in pool.
		/// </summary>
		public int Count
		{
			get
			{
				return _pool?.Count ?? 0;
			}
		}

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
			bool taken = _pool?.TryTake(out value) ?? false;
			if (taken) _onTaken();
			return taken;
		}

		/// <summary>
		/// Attempts to extract an item from the pool but if non are available it creates a new one from the factory provided or the underlying generator.
		/// </summary>
		/// <param name="factory">An optional custom factory to use if no items are in the pool.</param>
		/// <returns></returns>
		public T Take(Func<T> factory = null)
		{
			if (_generator == null && factory == null)
				throw new ArgumentException("factory", "Must provide a factory if one was not provided at construction time.");

			return TryTake(out T value)
				? value
				: (factory ?? _generator).Invoke();
		}

		protected static Task ClearAndDisposeContents(ConcurrentBag<T> bag)
		{
			return bag?.ClearAsync(e => (e as IDisposable)?.Dispose());
		}

		protected static Task ClearAndDisposeContents(ref ConcurrentBag<T> bag)
		{
			return ClearAndDisposeContents(Interlocked.Exchange(ref bag, null));
		}

		/// <summary>
		/// Will clear out the pool.
		/// </summary>
		public Task Clear()
		{
			return _pool?.ClearAsync(e => (e as IDisposable)?.Dispose());
		}

		protected virtual void OnDumping()
		{

		}

		/// <summary>
		/// Replaces current bag with a new one and returns the existing bag without disposing the contents.
		/// </summary>
		/// <returns>The previous ConcurrentBag<T>.</returns>
		public ConcurrentBag<T> Dump()
		{
			AssertIsAlive();
			OnDumping();
			var p = Interlocked.Exchange(ref _pool, new ConcurrentBag<T>());
			if (IsDisposed) // It is possible that a dispose was called immediately after exchanging the current pool.  In that case, ensure the pool is nullified and disposed.
				ClearAndDisposeContents(ref _pool); 
			return p ?? new ConcurrentBag<T>();
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			_generator = null;
			ClearAndDisposeContents(ref _pool);
			_recycler = null;
		}

		/// <summary>
		/// Receives an item and adds it to the pool. Ignores null references.
		/// WARNING: The item is considered 'dead' but resurrectable so be sure not to hold on to the item's reference.
		/// </summary>
		/// <param name="item">The item to give up to the pool.</param>
		public virtual void Give(T item)
		{
			if (item == null) return;

			var r = _recycler;
			var p = _pool;
			if (p==null || p.Count >= _maxSize)
			{
				// Getting too big, dispose immediately...
				(item as IDisposable)?.Dispose();
			}
			else
			{
				r?.Invoke(item);
				p.Add(item);
			}
		}

		/// <summary>
		/// Receives items and iteratively adds them to the pool.
		/// WARNING: These items are considered 'dead' but resurrectable so be sure not to hold on to their reference.
		/// </summary>
		/// <param name="items">The items to give up to the pool.</param>
		public void Give(IEnumerable<T> items)
		{
			if(items!=null)
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
