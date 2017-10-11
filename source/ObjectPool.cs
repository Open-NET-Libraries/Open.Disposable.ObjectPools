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
	public class ObjectPool<T> : SimpleObjectPool<T>
		where T : class
	{
		ActionRunner _trimmer;
		ActionRunner _flusher;
		ActionRunner _autoFlusher;

		/**
		 * A transient amount of object to exist over MaxSize until trim() is called.
		 * But any added objects over _localAbsMaxSize will be disposed immediately.
		 */
		uint _localAbsMaxSize;

		public ObjectPool(
			ushort maxSize,
			Func<T> generator,
			Action<T> recycler = null,
			TimeSpan? autoClearTimeout = null) : base(maxSize, generator, recycler)
		{
			_localAbsMaxSize = Math.Min((uint)maxSize * 2, ushort.MaxValue);
			_autoClearTimeout = autoClearTimeout ?? TimeSpan.FromSeconds(5);

			_trimmer = new ActionRunner(TrimInternal);
			_flusher = new ActionRunner(ClearInternal);
			_autoFlusher = new ActionRunner(ClearInternal);
		}

		TimeSpan _autoClearTimeout;
		/// <summary>
		/// By default will clear after 5 seconds of non-use.
		/// </summary>
		public TimeSpan AutoClearTimeout => _autoClearTimeout;

		protected override void _onTaken()
		{
			var len = _pool.Count;
			if(len<=_maxSize)
				_trimmer?.Cancel();
			if(len!=0)
				ExtendAutoClearInternal();
		}

		protected void TrimInternal()
		{
			_trimmer?.Cancel();
			_autoFlusher?.Cancel();

			var p = _pool;
			if (p!=null && p.Count > 0)
			{
				try
				{
					foreach (var e in p.TryTakeWhile(b => b.Count > _maxSize))
						(e as IDisposable)?.Dispose();
				}
				finally
				{
					ExtendAutoClearInternal();
				}
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
		public Task Trim(int defer = 0)
		{
			AssertIsAlive();
			return _trimmer?.Defer(defer);
		}

		protected void ClearInternal()
		{
			//Debug.WriteLine("ObjectPool cleared.");
			ClearAndDisposeContents(Dump());
		}

		/// <summary>
		/// Will clear out the pool.
		/// Cancels any scheduled trims when executed.
		/// </summary>
		/// <param name="defer">A delay before clearing.  Will be overridden by later calls.</param>
		/// <returns></returns>
		public Task Clear(int defer)
		{
			AssertIsAlive();
			return _flusher?.Defer(defer);
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
			if(!IsDisposed) // IsDisposed can happen much earlier so look for it before continuing...
				_autoFlusher?.Defer(_autoClearTimeout);
		}

		/// <summary>
		/// Receives an item and adds it to the pool. Ignores null references.
		/// WARNING: The item is considered 'dead' but resurrectable so be sure not to hold on to the item's reference.
		/// </summary>
		/// <param name="item">The item to give up to the pool.</param>
		public override void Give(T item)
		{
			if (item == null) return;

			var p = _pool;
			if (p==null || p.Count >= _localAbsMaxSize)
			{
				// Getting too big, dispose immediately...
				(item as IDisposable)?.Dispose();
			}
			else
			{
				_recycler?.Invoke(item);
				p.Add(item);
				if (p.Count > _maxSize)
					_trimmer?.Defer(500);
			}

			ExtendAutoClearInternal();
		}
		
	}
}
