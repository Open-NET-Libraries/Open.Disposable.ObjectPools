using System;
using System.Collections;
using System.Collections.Generic;

namespace Open.Disposable;

/*
 * There are two class varations here because ICollection and ICollection<T> do not overlap.
 * 'Count' is the common property used in both classes.
 */

public abstract class TrimmableCollectionObjectPoolBase<T, TCollection>
	: TrimmableObjectPoolBase<T>
	where T : class
	where TCollection : class, ICollection
{
	protected TrimmableCollectionObjectPoolBase(
		TCollection pool,
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY,
		bool countTrackingEnabled = true)
		: base(factory, recycler, disposer, capacity, countTrackingEnabled)
		=> Pool = pool ?? throw new ArgumentNullException(nameof(pool));

	protected TCollection Pool;

	/// <inheritdoc />
	public override int Count => (Pool?.Count ?? 0) + PocketCount;

	protected override void OnDispose()
	{
		base.OnDispose();
		Pool = null!;
	}
}

public abstract class TrimmableGenericCollectionObjectPoolBase<T, TCollection>
	: TrimmableObjectPoolBase<T>
	where T : class
	where TCollection : class, ICollection<T>
{
	protected TrimmableGenericCollectionObjectPoolBase(
		TCollection pool,
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY,
		bool countTrackingEnabled = true)
		: base(factory, recycler, disposer, capacity, countTrackingEnabled)
		=> Pool = pool ?? throw new ArgumentNullException(nameof(pool));

	protected TCollection Pool;

	/// <inheritdoc />
	public override int Count => (Pool?.Count ?? 0) + PocketCount;

	protected override void OnDispose()
	{
		//base.OnDispose(); // Do not call because the following is more optimized.

		var p = Pool;
		Pool = null!;

		if (OnDiscarded is not null)
		{
			foreach (var item in p)
				OnDiscarded(item);
		}

		p.Clear();
	}
}
