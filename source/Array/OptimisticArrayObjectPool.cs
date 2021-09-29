/* Based on Roslyn's ObjectPool */

using System;
using System.Runtime.CompilerServices;

namespace Open.Disposable;

/// <summary>
/// An extremely fast ObjectPool when the capacity is in the low 100s.
/// </summary>
/// <typeparam name="T"></typeparam>
public class OptimisticArrayObjectPool<T>
	: InterlockedArrayObjectPool<T>
	where T : class
{
	public OptimisticArrayObjectPool(
		Func<T> factory,
		Action<T>? recycler,
		int capacity = DEFAULT_CAPACITY)
		: base(factory, recycler, null /* disposer not applicable here */, capacity) { }

	public OptimisticArrayObjectPool(
		Func<T> factory,
		int capacity = DEFAULT_CAPACITY)
		: this(factory, null, capacity) { }

	// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override bool SaveToPocket(T item)
		=> Pocket.SetIfNull(item);

	// As suggested by Roslyn's implementation, don't worry about interlocking here.  It's okay if a few get loose.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override bool Store(ReferenceContainer<T>[] p, T item, int index)
		=> p[index].SetIfNull(item);
}

public static class OptimisticArrayObjectPool
{
	public static OptimisticArrayObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static OptimisticArrayObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static OptimisticArrayObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, capacity);

	public static OptimisticArrayObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);
}
