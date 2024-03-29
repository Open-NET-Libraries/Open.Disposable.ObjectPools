﻿/* Based on Roslyn's ObjectPool */

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Open.Disposable;

/// <summary>
/// An extremely fast ObjectPool when the capacity is in the low 100s.
/// </summary>
/// <typeparam name="T"></typeparam>
public class InterlockedArrayObjectPool<T>
	: ObjectPoolBase<T>
	where T : class
{
	public InterlockedArrayObjectPool(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY)
		: base(factory, recycler, disposer, capacity)
		=> Pool = new ReferenceContainer<T>[capacity - 1];

	public InterlockedArrayObjectPool(
		Func<T> factory,
		int capacity = DEFAULT_CAPACITY)
		: this(factory, null, null, capacity) { }

	protected ReferenceContainer<T>[] Pool;

	// Sets a limit on what has been stored yet to prevent over searching the array unnecessarily.. 
	protected int MaxStored;
	protected const int MaxStoredIncrement = 5; // Instead of every one.

	public override int Count
		=> Pool.Count(e => e.Value is not null) + PocketCount;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual bool Store(ReferenceContainer<T>[] p, T item, int index)
		=> p[index].TrySave(item);

	protected override bool Receive(T item)
	{
		var elements = Pool;
		var len = elements?.Length ?? 0;

		for (var i = 0; i < len; i++)
		{
			if (!Store(elements!, item, i)) continue;
			var m = MaxStored;
			if (i >= m) Interlocked.CompareExchange(ref MaxStored, m + MaxStoredIncrement, m);
			return true;
		}

		return false;
	}

	protected override T? TryRelease()
	{
		// We missed getting the first item or it wasn't there.
		var elements = Pool;
		if (elements is null) return null;

		var len = elements.Length;
		for (var i = 0; i < MaxStored && i < len; i++)
		{
			var item = elements[i].TryRetrieve();
			if (item is not null) return item;
		}

		return null;
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		Pool = null!;
		MaxStored = 0;
	}
}

public static class InterlockedArrayObjectPool
{
	public static InterlockedArrayObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static InterlockedArrayObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static InterlockedArrayObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, null, capacity);

	public static InterlockedArrayObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);

	public static InterlockedArrayObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable => new(factory, null, d => d.Dispose(), capacity);

	public static InterlockedArrayObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable, new() => CreateAutoDisposal(() => new T(), capacity);
}
