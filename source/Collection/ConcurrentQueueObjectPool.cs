﻿using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Open.Disposable;

public class ConcurrentQueueObjectPool<T>
	: TrimmableCollectionObjectPoolBase<T, ConcurrentQueue<T>>
	where T : class
{
	public ConcurrentQueueObjectPool(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY)
		: base(new ConcurrentQueue<T>(), factory, recycler, disposer, capacity) { }

	public ConcurrentQueueObjectPool(
		Func<T> factory,
		int capacity = DEFAULT_CAPACITY)
		: this(factory, null, null, capacity) { }

	/*
	 * NOTE: ConcurrentQueue is very fast and will perform quite well without using the 'Pocket' feature.
	 * Benchmarking reveals that mixed read/writes (what really matters) are still faster with the pocket enabled so best to keep it so.
	 */

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override bool Receive(T item)
	{
		var p = Pool;
		if (p is null) return false;
		p.Enqueue(item); // It's possible that the count could exceed MaxSize here, but the risk is negligble as a few over the limit won't hurt.
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override T? TryRelease()
	{
		var p = Pool;
		if (p is null) return null;
		p.TryDequeue(out var item);
		return item;
	}
}
public static class ConcurrentQueueObjectPool
{
	public static ConcurrentQueueObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static ConcurrentQueueObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static ConcurrentQueueObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, null, capacity);

	public static ConcurrentQueueObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);

	public static ConcurrentQueueObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable => new(factory, null, d => d.Dispose(), capacity);

	public static ConcurrentQueueObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable, new() => CreateAutoDisposal(() => new T(), capacity);
}
