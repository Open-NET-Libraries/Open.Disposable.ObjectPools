using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Open.Disposable;

public abstract class CountTrackedObjectPoolBase<T>
	: ObjectPoolBase<T>
	where T : class
{
	protected CountTrackedObjectPoolBase(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY)
		: base(factory, recycler, disposer, capacity) { }

	int _count;
	/// <inheritdoc />
	public override int Count => _count;

	protected override bool CanReceive => _count < MaxSize;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override void OnReleased() => Interlocked.Decrement(ref _count);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override void OnReceived() => Interlocked.Increment(ref _count);
}
