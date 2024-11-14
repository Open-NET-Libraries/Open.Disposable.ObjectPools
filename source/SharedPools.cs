using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable;

public sealed class SharedPool<T>(
	Func<T> factory,
	Action<T>? recycler,
	int capacity)
	: OptimisticArrayObjectPool<T>(factory, recycler, capacity)
	where T : class
{
	[Obsolete("Shared pools do not support disposal.")]
	public new void Dispose()
		=> throw new NotSupportedException("Shared pools cannot be disposed.");
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
	public static SharedPool<List<T>> Create(int capacity = Constants.DEFAULT_CAPACITY) => new(() => [], h =>
	{
		h.Clear();
		if (h.Capacity > 16) h.Capacity = 16;
	}, capacity);

	/// <summary>
	/// Provides a disposable RecycleHelper that contains an item from the pool.<br/>
	/// Will be returned to the pool once .Dispose() is called.
	/// </summary>
	/// <returns>A RecycleHelper containing an item from the pool.</returns>
	public static RecycleHelper<List<T>> Rent() => Shared.Rent();
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
		=> new(() => [], h => h.Clear(), capacity);

	/// <inheritdoc cref="ListPool{T}.Rent"/>
	public static RecycleHelper<HashSet<T>> Rent() => Shared.Rent();
}

public static class StringBuilderPool
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
		=> new(() => new(), sb => sb.Clear(), capacity);

	/// <summary>
	/// Provides a StringBuilder to be used for processing and finalizes by calling .ToString() and returning the value.
	/// </summary>
	/// <exception cref="ArgumentNullException">If either the pool or action are null.</exception>
	public static string RentToString(this IObjectPool<StringBuilder> pool, Action<StringBuilder> action)
	{
		if (pool is null) throw new ArgumentNullException(nameof(pool));
		if (action is null) throw new ArgumentNullException(nameof(action));

		var sb = pool.Take();
		action(sb);
		var result = sb.ToString();
		pool.Give(sb);
		return result;
	}

	/// <exception cref="ArgumentNullException">If the action is null.</exception>
	/// <inheritdoc cref="RentToString(IObjectPool{StringBuilder}, Action{StringBuilder})" />
	public static string RentToString(Action<StringBuilder> action)
		=> Shared.RentToString(action);

	/// <summary>
	/// Provides a disposable RecycleHelper that contains a StringBuilder from the pool.<br/>
	/// Will be returned to the pool once .Dispose() is called.
	/// </summary>
	/// <returns>A RecycleHelper containing a StringBuilder from the pool.</returns>
	public static RecycleHelper<StringBuilder> Rent() => Shared.Rent();
}

public static class DictionaryPool<TKey, TValue>
	where TKey : notnull
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
		=> new(() => [], h => h.Clear(), capacity);

	/// <summary>
	/// Provides a disposable RecycleHelper that contains an item from the pool.<br/>
	/// Will be returned to the pool once .Dispose() is called.
	/// </summary>
	/// <returns>A RecycleHelper containing a item from the pool.</returns>
	public static RecycleHelper<Dictionary<TKey, TValue>> Rent() => Shared.Rent();
}
