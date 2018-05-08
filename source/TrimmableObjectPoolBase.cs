using System;
using System.Diagnostics;
using System.Threading;

namespace Open.Disposable
{
	[DebuggerDisplay("Count = {Count}")]
	public abstract class TrimmableObjectPoolBase<T> : ObjectPoolBase<T>, ITrimmableObjectPool
		where T : class
	{
		protected TrimmableObjectPoolBase(Func<T> factory, Action<T> recycler, Action<T> disposer, int capacity, bool countTrackingEnabled = true)
			: base(factory, recycler, disposer, capacity)
		{
			_countTrackingEnabled = countTrackingEnabled;
		}

		int _count = 0;
		bool _countTrackingEnabled; // When true this enables tracking the number of entries entering and exiting the pool instead of calling '.Count'.  

		protected int CountInternal => _countTrackingEnabled ? _count : Count;

		protected override bool CanReceive => CountInternal < MaxSize;

		/// <summary>
		/// Signal for when an item was taken (actually removed) from the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent Released;
		protected void OnReleased(int newSize)
		{
			Debug.Assert(newSize > -2, $"newSize: {newSize}, _count: {_count}"); // Should never get out of control.  It may go negative temporarily but should be 100% accounted for.
			Released?.Invoke(newSize);
		}
		protected override void OnReleased()
		{
			OnReleased(_countTrackingEnabled ? Interlocked.Decrement(ref _count) : Count);
		}

		/// <summary>
		/// Signal for when an item was given (actually accepted) to the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent Received;
		protected void OnReceived(int newSize)
		{
			Received?.Invoke(newSize);
		}

		protected override void OnReceived()
		{
			OnReceived(_countTrackingEnabled ? Interlocked.Increment(ref _count) : Count);
		}

		public virtual void TrimTo(int targetSize)
		{
			if (targetSize < 0) return; // Possible upstream math hiccup or default -1.  Silently dismiss.
			int count = CountInternal;

			if (count > targetSize)
			{
				int i = 0; // Prevent an possiblility of indefinite loop.
				for (
					var attempts = count - targetSize;
					i < attempts;
					i++)
				{
					var e = TryRelease();
					if (e == null) break;

					if (_countTrackingEnabled)
					{
						Interlocked.Decrement(ref _count);
						//var c = Interlocked.Decrement(ref _count);
						//Debug.Assert(c >= 0);
					}

					OnDiscarded?.Invoke(e);
				}

				if (i != 0) OnReleased(CountInternal);
			}
		}

		//protected ReferenceContainer<T> Pocket2; // Default struct constructs itself.

		//protected override bool SaveToPocket(T item)
		//	=> Pocket.TrySave(item) || Pocket2.TrySave(item);

		//protected override T TakeFromPocket()
		//	=> Pocket.TryRetrieve() ?? Pocket2.TryRetrieve();

		//protected override int PocketCount =>
		//	base.PocketCount + (Pocket2.Value == null ? 0 : 1);
	}
}
