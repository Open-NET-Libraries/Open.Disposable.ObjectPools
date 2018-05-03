using System;
using System.Diagnostics;
using System.Threading;

namespace Open.Disposable
{
	[DebuggerDisplay("Count = {_count}")]
	public abstract class TrimmableObjectPoolBase<T> : ObjectPoolBase<T>, ITrimmableObjectPool
		where T : class
	{
		protected TrimmableObjectPoolBase(Func<T> factory, Action<T> recycler, int capacity, bool countTrackingEnabled)
			: base(factory, recycler, capacity)
		{
			_countTrackingEnabled = countTrackingEnabled;
		}

		protected TrimmableObjectPoolBase(Func<T> factory, int capacity, bool countTrackingEnabled)
			: this(factory, null, capacity, countTrackingEnabled)
		{
		}

		bool _countTrackingEnabled;
		public bool CountTrackingEnabled => _countTrackingEnabled;


		int _count;

		/// <summary>
		/// Total number of items in the pool.
		/// </summary>
		public virtual int Count => _count; // Track this number instead of calling .Count on a collection.

		protected override bool CanReceive => _count < MaxSize;

		/// <summary>
		/// Signal for when an item was taken (actually removed) from the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent Released;
		protected void OnReleased(int newSize)
		{
			if (_countTrackingEnabled)
			{
				Released?.Invoke(newSize);
			}

		}
		protected override void OnReleased()
		{
			if (_countTrackingEnabled)
			{
				var c = Interlocked.Decrement(ref _count);
				OnReleased(c);
				Debug.Assert(c >= 0);
			}
		}

		/// <summary>
		/// Signal for when an item was given (actually accepted) to the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent Received;
		protected void OnReceived(int newSize)
		{
			if (_countTrackingEnabled)
			{
				Received?.Invoke(newSize);
			}
		}

		protected override void OnReceived()
		{
			if (_countTrackingEnabled)
			{
				OnReceived(Interlocked.Increment(ref _count));
			}
		}

		public virtual void TrimTo(int targetSize)
		{
			if (!_countTrackingEnabled)
				throw new InvalidOperationException("Cannot trim an object pool with count tracking disabled.");

			if (targetSize < 0) return; // Possible upstream math hiccup or default -1.  Silently dismiss.
			int count = _count;

			if (count > targetSize)
			{
				int i = 0; // Prevent an possiblility of indefinite loop.
				for (
					var attempts = count - targetSize;
					i < attempts;
					i++)
				{
					if (TryRelease() == null)
						break;

					Interlocked.Decrement(ref _count);
				}

				if (i != 0) OnReleased(_count);
			}
		}

	}
}
