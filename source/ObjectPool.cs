using Open.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// An auto-trimming and optionally auto-clearing ObjectPool.
	/// When .Take() is called, it will return the first possible item even if one is returned to the pool before the generator function completes.
	/// Also does not block when .Give(T item) is called and items are recycled asynchronously.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public class ObjectPool<T> : RecyclableObjectPool<T>
		where T : class
	{
		ActionRunner _trimmer;
		ActionRunner _flusher;
		ActionRunner _autoFlusher;

		// micro-optimization for retrieving this value as read-only is slightly slower.
		ushort _trimmedSize;
		TimeSpan _autoClearTimeout;

		/// <summary>
		/// Constructs an auto-trimming and optionally auto-clearing ObjectPool.
		/// </summary>
		/// <param name="trimmedSize">The target size to limit to after a half second timeout.  Allowing the pool to still grow to the max size until the trim occurs.</param>
		/// <param name="generator">The generator function that creates the items.</param>
		/// <param name="recycler">An optional recycler function that will process items before making them available.</param>
		/// <param name="autoClearTimeout">An inactivity timeout for when to clear then entire pool.</param>
		/// <param name="maxSize">The maximum size of the object pool.  Default is ushort.MaxValue (65535).</param>
		public ObjectPool(
			ushort trimmedSize,
			Func<T> generator,
			Action<T> recycler = null,
			TimeSpan? autoClearTimeout = null,
			ushort maxSize = ushort.MaxValue) : base(generator, recycler, maxSize)
		{
			TrimmedSize = _trimmedSize = trimmedSize;
			AutoClearTimeout = _autoClearTimeout = autoClearTimeout ?? TimeSpan.Zero; // Delay to wait before clearing. None?  Then don't ever auto-clear (default).

			_trimmer = new ActionRunner(TrimInternal);
			_flusher = new ActionRunner(Clear);
			if (_autoClearTimeout > TimeSpan.Zero)
				_autoFlusher = new ActionRunner(Clear);
		}

		/// <summary>
		/// If value is not zero then the pool will clear after this inactivity timeout.
		/// </summary>
		public readonly TimeSpan AutoClearTimeout;

		/// <summary>
		///  Max size that trimming will allow.
		/// </summary>
		public readonly ushort TrimmedSize;

		protected override void _onTaken()
		{
			var len = Count;
			if (len <= _trimmedSize)
				_trimmer?.Cancel();
			if (len != 0)
				ExtendAutoClearInternal();
		}

		protected void TrimInternal()
		{
			_trimmer?.Cancel();
			_autoFlusher?.Cancel();

			if (Count > 0)
			{

				while (Count > _trimmedSize)
				{
					// First try to get any to be recycled items over the trim amount to avoid unnecessary recycling.
					var r = _recycleQueue;
					if (r != null && r.TryReceive(out T rItem))
					{
						QueueDisposal(rItem);
						continue;
					}

					var p = _pool;
					if (p == null) return;
					if (p.TryReceive(out T pItem))
					{
						QueueDisposal(pItem);
					}
				}

				ExtendAutoClearInternal();

			}
			else
			{
				_flusher?.Cancel();
			}
		}

		/// <summary>
		/// Will initiate trimming the pool to ensure it is less than the maxSize.
		/// </summary>
		/// <param name="defer">Optional millisecond value to wait until trimming starts.</param>
		/// <returns></returns>
		public Task<bool> Trim(int defer = 0)
		{
			AssertIsAlive();
			return _trimmer?.Defer(defer).ContinueWith(t => t.Status == TaskStatus.RanToCompletion);
		}

		/// <summary>
		/// Will clear out the pool.
		/// Cancels any scheduled trims when executed.
		/// </summary>
		/// <param name="defer">A delay before clearing.  Will be overridden by later calls.</param>
		/// <returns></returns>
		public Task<bool> Clear(int defer)
		{
			AssertIsAlive();
			return _flusher?.Defer(defer).ContinueWith(t => t.Status == TaskStatus.RanToCompletion);
		}

		protected override void OnDumping()
		{
			_trimmer?.Cancel();
			_flusher?.Cancel();
			_autoFlusher?.Cancel();
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			DisposeOf(ref _trimmer);
			DisposeOf(ref _flusher);
			DisposeOf(ref _autoFlusher);

			base.OnDispose(calledExplicitly);
		}

		/// <summary>
		/// Simply extends the auto-clear sequence from now until later by the AutoClearTimeout value.
		/// </summary>
		public void ExtendAutoClear()
		{
			AssertIsAlive();
			_autoFlusher?.Defer(_autoClearTimeout);
		}

		void ExtendAutoClearInternal()
		{
			if (!IsDisposed) // IsDisposed can happen much earlier so look for it before continuing...
				_autoFlusher?.Defer(_autoClearTimeout);
		}

		protected override bool GiveInternal(T item)
		{
			var wasGiven = base.GiveInternal(item);
			if (wasGiven)
			{
				if (Count > _trimmedSize)
					_trimmer?.Defer(500);

				ExtendAutoClearInternal();
			}
			return wasGiven;
		}

	}
}
