using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Open.Disposable
{
	public abstract class ObjectPoolBase<T> : DisposableBase, IObjectPool<T>
		where T : class
	{
		protected const int DEFAULT_CAPACITY = Constants.DEFAULT_CAPACITY;

		protected ObjectPoolBase(Func<T> factory, Action<T>? recycler, Action<T>? disposer, int capacity = DEFAULT_CAPACITY)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Must be at least 1.");
			Factory = factory ?? throw new ArgumentNullException(nameof(factory));
			MaxSize = capacity;
			Recycler = recycler;
			OnDiscarded = disposer;
		}

		// Not read-only due to quirk where read-only is slower than not.
		protected int MaxSize;
		public int Capacity => MaxSize;

		/// <summary>
		/// Total number of items in the pool.
		/// </summary>
		/// <inheritdoc />
		public abstract int Count { get; }
		protected int PocketCount => Pocket.Value is null ? 0 : 1;

		protected readonly Action<T>? Recycler; // Before entering the pool.
		protected readonly Action<T>? OnDiscarded; // When not able to be used.


		// Read-only because if Take() is called after disposal, this still facilitates returing an object.
		// Allow the GC to do the final cleanup after dispose.
		protected readonly Func<T> Factory;

		public T Generate() => Factory();

		// ReSharper disable once UnassignedField.Global
		protected ReferenceContainer<T> Pocket; // Default struct constructs itself.

		#region Receive (.Give(T item))
		protected virtual bool CanReceive => true; // A default of true is acceptable, enabling the Receive method to do the actual deciding. 

		protected bool PrepareToReceive(T item)
		{
			if (CanReceive && item is not null)
			{
				var r = Recycler;
				if (r is not null)
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

		/// <inheritdoc />
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
		/// <inheritdoc />
		public virtual T Take()
			=> TryTake() ?? Factory();

		/// <inheritdoc />
#if NETSTANDARD2_1
		public bool TryTake([NotNullWhen(true)] out T? item)
#else
		public bool TryTake(out T? item)
#endif
			=> (item = TryTake()) is not null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual bool SaveToPocket(T item)
			=> Pocket.TrySave(item);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T? TakeFromPocket()
			=> Pocket.TryRetrieve();


		protected abstract T? TryRelease();

		/// <inheritdoc />
		public T? TryTake()
		{
			var item = TakeFromPocket() ?? TryRelease();
			if (item is not null) OnReleased();
			return item;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual void OnReleased()
		{
		}
		#endregion

		protected override void OnBeforeDispose()
		{
			MaxSize = 0;
		}

		protected override void OnDispose()
		{
			if (OnDiscarded is null) return;

			T? d;
			while ((d = TryRelease()) is not null) OnDiscarded(d);
		}
	}
}
