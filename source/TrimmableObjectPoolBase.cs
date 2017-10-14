using Open.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

		/// <summary>
		/// Signal for when an item was taken (actually removed) from the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent TakenFrom;
		protected void OnTakenFrom(int newSize)
		{
			TakenFrom?.Invoke(newSize);
		}
		protected void OnTakenFrom(bool wasTaken)
		{
			if (wasTaken) OnTakenFrom();
		}
		protected virtual void OnTakenFrom()
		{
			OnTakenFrom(Count);
		}

		protected override bool CanGive(T item)
		{
			if (item == null) return false;

			if (Count >= MaxSize) return false;
			var r = Recycler;
			if (r != null)
			{
				r(item);
				if (Count >= MaxSize) return false;
			}

			return true;
		}

		/// <summary>
		/// Signal for when an item was given (actually accepted) to the pool. 
		/// </summary>
		public event ObjectPoolResizeEvent GivenTo;
		protected void OnGivenTo(int newSize)
		{
			GivenTo?.Invoke(newSize);
		}
		protected void OnGivenTo(bool wasGiven)
		{
			if (wasGiven) OnGivenTo();
		}
		protected virtual void OnGivenTo()
		{
			OnGivenTo(Count);
		}


		public sealed override void Give(T item)
		{
			if (GiveInternal(item))
				OnGivenTo();
		}

		public sealed override Task GiveAsync(T item)
		{
			return GiveInternalAsync(item)
				.OnFullfilled((Action<bool>)OnGivenTo);
		}

		public override T TryTake()
		{
			var item = TryTakeInternal();
			if (item != null) OnTakenFrom();
			return item;
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
				if (TryTakeInternal() == null)
					break;

				count = Count;
			}

			if (i != 0) OnTakenFrom(count);
		}

	}
}
