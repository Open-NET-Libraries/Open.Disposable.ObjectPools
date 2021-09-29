using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable;

public class SharedPool<T> : ConcurrentQueueObjectPoolSlimBase<T>
	where T : class
{
	public SharedPool(
		Func<T> factory,
		Action<T>? recycler,
		Action<T>? disposer,
		int capacity)
		: base(factory, recycler, disposer, capacity) { }

	[Obsolete("Shared pools do not support disposal.")]
	public new void Dispose()
	{
		throw new NotSupportedException("Shared pools cannot be disposed.");
	}
}

public static class ListPool<T>
{
	/// <summary>
	/// A shared object pool for use with lists.
	/// The list is cleared after being returned.
	/// The max size of the pool is 64 which should suffice for most use cases.
	/// </summary>
	public static readonly SharedPool<List<T>> Shared = Create();

	/// <summary>
	/// Creates an object pool for use with lists.
	/// The list is cleared after being returned.
	/// </summary>
	public static SharedPool<List<T>> Create(int capacity = Constants.DEFAULT_CAPACITY) => new(() => new(), h =>
	{
		h.Clear();
		if (h.Capacity > 16) h.Capacity = 16;
	}, null, capacity);
}

public static class HashSetPool<T>
{
	/// <summary>
	/// A shared object pool for use with hash-sets.
	/// The hash-set is cleared after being returned.
	/// The max size of the pool is 64 which should suffice for most use cases.
	/// </summary>
	public static readonly SharedPool<HashSet<T>> Shared = Create();

	/// <summary>
	/// Creates an object pool for use with hash-sets.
	/// The hash-set is cleared after being returned.
	/// </summary>
	public static SharedPool<HashSet<T>> Create(int capacity = Constants.DEFAULT_CAPACITY)
		=> new(() => new(), h => h.Clear(), null, capacity);
}

public static class StringBuilderPool<T>
{
	/// <summary>
	/// A shared object pool for use with StringBuilders.
	/// The StringBuilder is cleared after being returned.
	/// The max size of the pool is 64 which should suffice for most use cases.
	/// </summary>
	public static readonly SharedPool<StringBuilder> Shared = Create();

	/// <summary>
	/// Creates an object pool for use with StringBuilders.
	/// The StringBuilder is cleared after being returned.
	/// </summary>
	public static SharedPool<StringBuilder> Create(int capacity = Constants.DEFAULT_CAPACITY)
		=> new(() => new(), sb => sb.Clear(), null, capacity);
}

public static class DictionaryPool<TKey, TValue>
{
	/// <summary>
	/// A shared object pool for use with dictionaries.
	/// The dictionary is cleared after being returned.
	/// The max size of the pool is 64 which should suffice for most use cases.
	/// </summary>
	public static readonly SharedPool<Dictionary<TKey, TValue>> Shared = Create();

	/// <summary>
	/// Creates an object pool for use with dictionaries.
	/// The dictionary is cleared after being returned.
	/// </summary>
	public static SharedPool<Dictionary<TKey, TValue>> Create(int capacity = Constants.DEFAULT_CAPACITY)
		=> new(() => new Dictionary<TKey, TValue>(), h => h.Clear(), null, capacity);

}
