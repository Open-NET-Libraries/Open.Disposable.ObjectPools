﻿using System;
using System.Collections.Generic;

namespace Open.Disposable;

public sealed class StackObjectPool<T>
	: TrimmableCollectionObjectPoolBase<T, Stack<T>>
	where T : class
{
	public StackObjectPool(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY)
		: base(
			  new Stack<T>(Math.Min(DEFAULT_CAPACITY, capacity)) /* Very very slight speed improvment when capacity is set. */,
			  factory, recycler, disposer, capacity, false)
	{ }

	public StackObjectPool(
		Func<T> factory,
		int capacity = DEFAULT_CAPACITY)
		: this(factory, null, null, capacity) { }

	protected override bool Receive(T item)
	{
		var p = Pool;
		if (p is not null)
		{
			// It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
			// The lock operation should be quick enough to not pile up too many items.
			lock (SyncRoot) p.Push(item);
			return true;
		}

		return false;
	}

	protected override T? TryRelease()
	{
		var p = Pool;
		if (p is null || p.Count == 0)
			return null;

		lock (SyncRoot)
		{
			if (p.Count != 0)
				return p.Pop();
		}

		return null;
	}
}

public static class StackObjectPool
{
	public static StackObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static StackObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static StackObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, null, capacity);

	public static StackObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);

	public static StackObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable => new(factory, null, d => d.Dispose(), capacity);

	public static StackObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable, new() => CreateAutoDisposal(() => new T(), capacity);
}
