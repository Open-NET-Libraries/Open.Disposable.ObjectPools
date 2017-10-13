using Open.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Open.Disposable
{
	public abstract class ObjectPoolBase<T> : DisposableBase, IObjectPool<T>
		where T : class
	{
		protected const int DEFAULT_CAPACITY = Constants.DEFAULT_CAPACITY;

		protected ObjectPoolBase(Func<T> factory, int capacity = DEFAULT_CAPACITY)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException("capacity", capacity, "Must be at least 1.");
			Factory = factory ?? throw new ArgumentNullException("factory");
			MaxSize = capacity;
		}


		protected int MaxSize;
		public int Capacity => MaxSize;

		// Read-only because if Take() is called after disposal, this still facilitates returing an object.
		// Allow the GC to do the final cleanup after dispose.
		protected readonly Func<T> Factory;

		protected abstract bool GiveInternal(T item);

		public virtual void Give(T item)
		{
			GiveInternal(item);
		}

		protected virtual Task<bool> GiveInternalAsync(T item)
		{
			return Task.Run(() => GiveInternal(item));
		}

		public virtual Task GiveAsync(T item)
		{
			return GiveInternalAsync(item);
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

		public virtual bool TryTake(out T item)
		{
			item = TryTake();
			return item != null;
		}

		protected abstract T TryTakeInternal();

		public virtual T TryTake()
		{
			return TryTakeInternal();
		}

		protected override void OnBeforeDispose()
		{
			MaxSize = 0;
		}
	}
}
