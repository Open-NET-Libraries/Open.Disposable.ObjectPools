using System;

namespace Open.Disposable
{
	public abstract class ObjectPoolBase<T> : DisposableBase, IObjectPool<T>
		where T : class
	{
		protected const int DEFAULT_CAPACITY = Constants.DEFAULT_CAPACITY;

		protected ObjectPoolBase(Func<T> factory, Action<T> recycler, Action<T> disposer, int capacity = DEFAULT_CAPACITY)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Must be at least 1.");
			Factory = factory ?? throw new ArgumentNullException(nameof(factory));
			MaxSize = capacity;
			Recycler = recycler;
			OnDiscarded = disposer;
		}

		protected int MaxSize;
		public int Capacity => MaxSize;

		protected Action<T> Recycler; // Before entering the pool.
		protected Action<T> OnDiscarded; // When not able to be used.


		// Read-only because if Take() is called after disposal, this still facilitates returing an object.
		// Allow the GC to do the final cleanup after dispose.
		protected readonly Func<T> Factory;

		public T Generate() => Factory();

		protected ReferenceContainer<T> Pocket; // Default struct constructs itself.

		#region Receive (.Give(T item))
		protected virtual bool CanReceive => true; // A default of true is acceptable, enabling the Recieve method to do the actual deciding. 

		protected bool PrepareToReceive(T item)
		{
			if (item == null) return false;

			if (CanReceive)
			{
				var r = Recycler;
				if (r != null)
				{
					r(item);
					// Did the recycle take so long that the state changed?
					if (!CanReceive) return false;
				}

				return true;
			}

			return false;
		}

		// Contract should be that no item can be null here.
		protected abstract bool Receive(T item);

		protected virtual void OnReceived()
		{

		}

		protected void OnReceived(bool wasGiven)
		{
			if (wasGiven) OnReceived();
		}

		public void Give(T item)
		{
			if (PrepareToReceive(item)
				&& (SaveToPocket(item) || Receive(item)))
				OnReceived();
			else
				OnDiscarded?.Invoke(item);
		}
		#endregion

		#region Release (.Take())
		public virtual T Take()
		{
			return TryTake() ?? Factory();
		}

		public bool TryTake(out T item)
		{
			item = TryTake();
			return item != null;
		}

		protected virtual bool SaveToPocket(T item)
			=> Pocket.TrySave(item);

		protected virtual T TakeFromPocket()
			=> Pocket.TryRetrieve();


		protected abstract T TryRelease();

		public T TryTake()
		{
			var item = TakeFromPocket() ?? TryRelease();
			if (item != null) OnReleased();
			return item;
		}

		protected virtual void OnReleased()
		{
		}
		#endregion

		protected override void OnBeforeDispose()
		{
			MaxSize = 0;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			if (calledExplicitly && OnDiscarded != null)
			{
				T d;
				while ((d = TryRelease()) != null) OnDiscarded(d);
			}
		}
	}
}
