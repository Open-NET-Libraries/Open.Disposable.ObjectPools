using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Open.Disposable;

public abstract class ConcurrentQueueObjectPoolSlimBase<T>
	: CountTrackedObjectPoolBase<T>
	where T : class
{
	protected ConcurrentQueueObjectPoolSlimBase(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY - 10)
		: base(factory, recycler, disposer, capacity)
		=> Pool = new();

	ConcurrentQueue<T> Pool;

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

	protected override void OnDispose()
	{
		base.OnDispose();
		Pool = null!;
	}
}
