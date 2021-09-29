using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Open.Disposable;

public sealed class ConcurrentQueueObjectPoolSlim<T>
	: ConcurrentQueueObjectPoolSlimBase<T>
	where T : class
{
	public ConcurrentQueueObjectPoolSlim(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY - 10)
	: base(factory, recycler, disposer, capacity) { }

	public ConcurrentQueueObjectPoolSlim(
		Func<T> factory,
		int capacity = DEFAULT_CAPACITY - 10)
		: this(factory, null, null, capacity) { }
}

public static class ConcurrentQueueObjectPoolSlim
{
	public static ConcurrentQueueObjectPoolSlim<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static ConcurrentQueueObjectPoolSlim<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static ConcurrentQueueObjectPoolSlim<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, null, capacity);

	public static ConcurrentQueueObjectPoolSlim<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);

	public static ConcurrentQueueObjectPoolSlim<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable => new(factory, null, d => d.Dispose(), capacity);

	public static ConcurrentQueueObjectPoolSlim<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable, new() => CreateAutoDisposal(() => new T(), capacity);

}
