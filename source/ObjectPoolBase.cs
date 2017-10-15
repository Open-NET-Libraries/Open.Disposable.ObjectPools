using Open.Threading;
using System;
using System.Diagnostics;
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

		protected virtual void OnGivenTo()
		{

		}
		protected void OnGivenTo(bool wasGiven)
		{
			if (wasGiven) OnGivenTo();
		}

		public void Give(T item)
		{
			if(PrepareToReceive(item) && (GaveToPocket(ref item) || Receive(item)))
				OnGivenTo();
		}

		protected virtual Task<bool> GiveInternalAsync(T item)
		{
			return Task.Run(() => Receive(item));
		}

		public virtual Task GiveAsync(T item)
		{
			if (item == null) return Task.FromResult(false);
			return GiveInternalAsync(item)
				.OnFullfilled((Action<bool>)OnGivenTo);
		}
		
		public virtual T Take()
		{
			return TryTake() ?? Factory();
		}

		protected virtual Task<T> TakeAsyncInternal()
		{
			return Task.Run((Func<T>)Take);
		}

		public Task<T> TakeAsync()
		{
			// See if there's one available already.
			if (TryTake(out T firstTry))
				return Task.FromResult(firstTry);

			return TakeAsyncInternal();
		}

		public bool TryTake(out T item)
		{
			item = TryTake();
			return item != null;
		}

		protected bool AllowPocket = true;

		protected T Pocket;
		protected T TakeFromPocket()
		{
			if (!AllowPocket || Pocket == null) return null;
			return Interlocked.Exchange(ref Pocket, null);
		}

		protected bool GaveToPocket(ref T item)
		{
			if (!AllowPocket || Pocket != null) return false;
			item = Interlocked.Exchange(ref Pocket, item);
			return item == null;
		}

		protected abstract T TryTakeInternal();

		public T TryTake()
		{
			var item = TakeFromPocket() ?? TryTakeInternal();
			if (item != null) OnTakenFrom();
			return item;
		}

		protected virtual void OnTakenFrom()
		{
		}
		protected void OnTakenFrom(bool wasTaken)
		{
			if (wasTaken) OnTakenFrom();
		}

		protected override void OnBeforeDispose()
		{
			MaxSize = 0;
		}
	}
}
