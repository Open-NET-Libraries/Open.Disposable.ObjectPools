﻿using System;

namespace Open.Disposable;

/// <summary>
/// Allows for the 'using' syntax to be used with any object to return it to the pool.
/// </summary>
public struct RecycleHelper<T> : IDisposable
	where T : class
{
	private readonly IObjectPool<T> _pool;

	private RecycleHelper(IObjectPool<T> pool, T item)
	{
		_pool = pool;
		_item = item;
	}

	public RecycleHelper(IObjectPool<T> pool)
		: this(pool ?? throw new ArgumentNullException(nameof(pool)), pool.Take())
	{
	}

	private T? _item;
	public readonly T Item
		=> _item ?? throw new ObjectDisposedException(GetType().ToString());

	public void Dispose()
	{
		var i = _item;
		_item = null;
		if (i is null) return;
		_pool.Give(i);
	}

	public static implicit operator T(RecycleHelper<T> helper) => helper.Item;
}
