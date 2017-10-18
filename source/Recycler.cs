﻿using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	/// <summary>
	/// This class is provided as an asynchronous queue for recycling instead of using a recycle delegate with an object pool and calling GiveAsync() which could pile up unnecessarily.
	/// So if recycling an object takes extra time, this might be a good way to toss objects away and not have to worry about the heavy cost as they will one by one be processed back into the target pool.
	/// </summary>
	public class Recycler<T> : DisposableBase
		where T : class
	{
		IObjectPool<T> _target;
		ActionBlock<T> _bin;

		public Recycler(			
			IObjectPool<T> target,
			Action<T> recycleFunction,
			ushort limit = ushort.MaxValue)
		{
			if(recycleFunction==null) throw new ArgumentNullException("recycleFunction");
			_target = target ?? throw new ArgumentNullException("target");

			_bin = new ActionBlock<T>(item =>
			{
				if (_target != null)
				{
					recycleFunction(item);
					_target?.Give(item);
				}
			});

			if (target is DisposableBase d)
			{
				if (d.IsDisposed) throw new ArgumentException("Cannot recycle for an object pool that is already disposed.");
				d.BeforeDispose += Pool_BeforeDispose;
				// Could possibly dispose before this line somewhere... But that's just nasty. :P  
			}
		}

		public Recycler(
			ushort limit,
			IObjectPool<T> pool,
			Action<T> recycleFunction) : this(pool, recycleFunction, limit)
		{

		}

		void Pool_BeforeDispose(object sender, EventArgs e)
		{
			Dispose();
		}

		public bool Recycle(T item)
		{
			return _bin?.Post(item) ?? false;
		}

		public Task Close()
		{
			var b = _bin;
			if (b == null) return Task.CompletedTask;
			b.Complete();
			return b.Completion;
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			_bin.Complete();
			_bin = null;
			_target = null;
		}
	}
}