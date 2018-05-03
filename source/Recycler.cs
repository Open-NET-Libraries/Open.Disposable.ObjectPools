﻿using System;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// This class is provided as an asynchronous queue for recycling instead of using a recycle delegate with an object pool and calling GiveAsync() which could pile up unnecessarily.
	/// So if recycling an object takes extra time, this might be a good way to toss objects away and not have to worry about the heavy cost as they will one by one be processed back into the target pool.
	/// </summary>
	public class Recycler<T> : RecyclerBase<T>
		where T : class
	{
		// Something else could be used and could be more performant.
		// But it's an ideal interface for what's needed.  And the point is that the recyler should not take up too much cpu time.
		ActionBlock<T> _bin;

		internal Recycler(
			IObjectPool<T> target,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY) : base(target, recycleFunction, limit)
		{
			_bin = new ActionBlock<T>(item =>
			{
				if (Target != null)
				{
					recycleFunction(item);
					Target?.Give(item);
				}
			});

			Completion = _bin.Completion;
		}

		internal Recycler(
			ushort limit,
			IObjectPool<T> pool,
			Action<T> recycleFunction) : this(pool, recycleFunction, limit)
		{

		}

		public override bool Recycle(T item)
		{
			return _bin?.Post(item) ?? false;
		}

		protected override void OnCloseRequested()
			=> _bin?.Complete();

		protected override void OnDispose(bool calledExplicitly)
		{
			base.OnDispose(calledExplicitly);
			if (calledExplicitly) _bin = null;
		}
	}

	public static class Recycler
	{
		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new Recycler<T>(pool, recycleFunction, limit);
		}

		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit,
			Action<T> recycleFunction)
			where T : class
		{
			return new Recycler<T>(pool, recycleFunction, limit);
		}

		public static void Recycle(IRecyclable r)
		{
			r.Recycle();
		}

		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable
		{
			return new Recycler<T>(pool, Recycle, limit);
		}

	}
}
