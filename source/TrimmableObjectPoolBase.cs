using System;
using System.Diagnostics;

namespace Open.Disposable
{
	[DebuggerDisplay("Count = {Count}")]
	public abstract class TrimmableObjectPoolBase<T> : ObjectPoolBase<T>, ITrimmableObjectPool
		where T : class
    {


		public TrimmableObjectPoolBase(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
		}

		public TrimmableObjectPoolBase(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{
		}

		/// <summary>
		/// Total number of items in the pool.
		/// </summary>
		public abstract int Count { get; }

		protected override bool CanReceive => Count < MaxSize;

		/// <summary>
		/// Signal for when an item was taken (actually removed) from the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent Released;
		protected void OnReleased(int newSize)
		{
			Released?.Invoke(newSize);
		}
		protected override void OnReleased()
		{
			OnReleased(Count);
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
			OnReceived(Count);
		}

		public virtual void TrimTo(int targetSize)
		{
			if (targetSize < 0) return; // Possible upstream math hiccup or default -1.  Silently dismiss.
			int count = Count;

			int i = 0; // Prevent an possiblility of indefinite loop.
			for (
				var attempts = count - targetSize;
				i < attempts && count > targetSize;
				i++)
			{
				if (TryRelease() == null)
					break;

				count = Count;
			}

			if (i != 0) OnReleased(count);
		}

	}
}
