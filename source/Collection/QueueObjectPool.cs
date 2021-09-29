using System;
using System.Collections.Generic;

namespace Open.Disposable;

public sealed class QueueObjectPool<T>
	: TrimmableCollectionObjectPoolBase<T, Queue<T>>
	where T : class
{
	public QueueObjectPool(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity = DEFAULT_CAPACITY)
		: base(
			  new Queue<T>(Math.Min(DEFAULT_CAPACITY, capacity)) /* Very very slight speed improvment when capacity is initially set. */,
			  factory, recycler, disposer, capacity, false)
	{ }

	public QueueObjectPool(
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
			lock (p) p.Enqueue(item);
			return true;
		}

		return false;
	}

	protected override T? TryRelease()
	{
		var p = Pool;
		if (p is not null && p.Count != 0)
		{
			lock (p)
			{
				if (p.Count != 0)
					return p.Dequeue();
			}

		}

		return null;
	}

}

public static class QueueObjectPool
{
	public static QueueObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class => new(factory, capacity);

	public static QueueObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, new() => Create(() => new T(), capacity);

	public static QueueObjectPool<T> CreateAutoRecycle<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		 where T : class, IRecyclable => new(factory, Recycler.Recycle, null, capacity);

	public static QueueObjectPool<T> CreateAutoRecycle<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IRecyclable, new() => CreateAutoRecycle(() => new T(), capacity);

	public static QueueObjectPool<T> CreateAutoDisposal<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable => new(factory, null, d => d.Dispose(), capacity);

	public static QueueObjectPool<T> CreateAutoDisposal<T>(int capacity = Constants.DEFAULT_CAPACITY)
		where T : class, IDisposable, new() => CreateAutoDisposal(() => new T(), capacity);

}
