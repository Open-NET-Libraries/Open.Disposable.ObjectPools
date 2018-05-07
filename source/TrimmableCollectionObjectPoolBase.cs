﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Open.Disposable
{
	public abstract class TrimmableCollectionObjectPoolBase<T, TCollection> : TrimmableObjectPoolBase<T>
		where T : class
		where TCollection : class, ICollection
	{

		public TrimmableCollectionObjectPoolBase(TCollection pool, Func<T> factory, Action<T> recycler, Action<T> disposer, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: base(factory, recycler, disposer, capacity, countTrackingEnabled)
		{
			Pool = pool ?? throw new ArgumentNullException(nameof(pool));
		}

		public TrimmableCollectionObjectPoolBase(TCollection pool, Func<T> factory, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: this(pool, factory, null, null, capacity, countTrackingEnabled)
		{
		}

		protected TCollection Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override void OnDispose(bool calledExplicitly)
		{
			base.OnDispose(calledExplicitly); // Do not call because the following is more optimized.
			if (calledExplicitly) Pool = null;
		}
	}

	public abstract class TrimmableGenericCollectionObjectPoolBase<T, TCollection> : TrimmableObjectPoolBase<T>
		where T : class
		where TCollection : class, ICollection<T>
	{

		public TrimmableGenericCollectionObjectPoolBase(TCollection pool, Func<T> factory, Action<T> recycler, Action<T> disposer, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: base(factory, recycler, disposer, capacity, countTrackingEnabled)
		{
			Pool = pool;
		}

		public TrimmableGenericCollectionObjectPoolBase(TCollection pool, Func<T> factory, int capacity = DEFAULT_CAPACITY, bool countTrackingEnabled = true)
			: this(pool, factory, null, null, capacity, countTrackingEnabled)
		{
		}

		protected TCollection Pool;

		public override int Count => Pool?.Count ?? 0;

		protected override void OnDispose(bool calledExplicitly)
		{
			//base.OnDispose(calledExplicitly); // Do not call because the following is more optimized.

			if (calledExplicitly)
			{
				var p = Pool;
				Pool = null;

				if (OnDiscarded != null)
				{
					foreach (var item in p)
						OnDiscarded(item);
				}

				p.Clear();
			}
		}
	}

}
