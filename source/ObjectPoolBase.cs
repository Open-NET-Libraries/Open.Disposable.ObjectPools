using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Disposable
{
	public abstract class ObjectPoolBase<T> : DisposableBase, IObjectPool<T>
		where T : class
	{
		protected const int DEFAULT_CAPACITY = Constants.DEFAULT_CAPACITY;

		protected ObjectPoolBase(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException("capacity", capacity, "Must be at least 1.");
			Factory = factory ?? throw new ArgumentNullException("factory");
			MaxSize = capacity;
			Recycler = recycler;
		}

		protected ObjectPoolBase(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{

		}

		protected Action<T> Recycler;
		protected int MaxSize;
		public int Capacity => MaxSize;

		// Read-only because if Take() is called after disposal, this still facilitates returing an object.
		// Allow the GC to do the final cleanup after dispose.
		protected readonly Func<T> Factory;

		public T Generate()
		{
			return Factory();
		}

        protected ReferenceContainer<T> Pocket;

        #region Receive (.Give(T item))
        protected virtual bool CanReceive => true;

		protected bool PrepareToReceive(T item)
		{
			if (item == null) return false;

			if (!CanReceive) return false;
			var r = Recycler;
			if (r != null)
			{
				r(item);
				if (!CanReceive) return false;
			}

			return true;
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
				&& (GiveToPocket(item) || Receive(item)))
				OnReceived();
		}

		protected virtual Task<bool> ReceiveAsync(T item)
		{
			return Task.Run(() => Receive(item));
		}

		public Task GiveAsync(T item)
		{
			// We need to pre-check CanReceive because excessive tasks could build up if not.
			if (item == null || !CanReceive)
                return Task.CompletedTask;
			
			return ReceiveConditionalAsync(item);
		}

		async Task ReceiveConditionalAsync(T item)
		{
			if(PrepareToReceive(item)
				&& (GiveToPocket(item) || await ReceiveAsync(item)))
				OnReceived();
		}
        #endregion

        #region Release (.Take())
        public virtual T Take()
		{
			return TryTake() ?? Factory();
		}

		protected virtual Task<T> ReleaseAsync()
		{
			return Task.Run((Func<T>)Take);
		}

		public Task<T> TakeAsync()
		{
			// See if there's one available already.
			if (TryTake(out T firstTry))
				return Task.FromResult(firstTry);

			return ReleaseAsync();
		}

		public bool TryTake(out T item)
		{
			item = TryTake();
			return item != null;
		}

        protected virtual bool GiveToPocket(T item)
        {
            return Pocket.TrySave(item);
        }

        protected virtual T TakeFromPocket()
        {
            return Pocket.TryRetrieve();
		}

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
		protected void OnReleased(bool wasTaken)
		{
			if (wasTaken) OnReleased();
		}
        #endregion

        protected override void OnBeforeDispose()
		{
			MaxSize = 0;
		}
	}
}
