using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Open.Disposable;

public interface IObjectPool<T>
	where T : class
{
	/// <summary>
	/// Defines the maximum at which the pool can grow.
	/// </summary>
	int Capacity { get; }

	/// <summary>
	/// Directly calls the underlying factory that generates the items.  (No pool interaction.)
	/// </summary>
	T Generate();

	/// <summary>
	/// Receives an item and adds it to the pool. Ignores null references.<br/>
	/// WARNING: The item is considered 'dead' but resurrectable so be sure not to hold on to the item's reference.
	/// </summary>
	/// <param name="item">The item to give up to the pool.</param>
	void Give(T item);

	/// <summary>
	/// If the pool has an item currently avaialable, removes it from the pool and provides it as the out parameter.
	/// </summary>
	/// <param name="item">The item to return if available.  Will be null if none avaialable.</param>
	/// <returns>True if an item is provided.</returns>
#if NETSTANDARD2_1
	bool TryTake([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? item);
#else
	bool TryTake(out T? item);
#endif

	/// <summary>
	/// If the pool has an item currently avaialable, removes it from the pool and returns it.
	/// </summary>
	/// <returns>The item to return if available.  Will be null if none avaialable.</returns>
	T? TryTake();

	/// <summary>
	/// If the pool has an item currently avaialable, removes it from the pool and returns it.
	/// If none is available, it generates one.
	/// </summary>
	/// <returns>An item removed from the pool or generated.  Should never be null.</returns>
	T Take();

	/// <summary>
	/// Total number of items in the pool.<br/>
	/// Depending on the implementation could be an O(n) operation and should only be used for debugging.
	/// </summary>
	int Count { get; }
}

public static class ObjectPoolExtensions
{
	/// <summary>
	/// Receives items and iteratively adds them to the pool.<br/>
	/// WARNING: These items are considered 'dead' but resurrectable so be sure not to hold on to their reference.
	/// </summary>
	/// <param name="target">The pool to give to.</param>
	/// <param name="items">The items to give up to the pool.</param>
	public static void Give<T>(this IObjectPool<T> target, IEnumerable<T> items)
		where T : class
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		Contract.EndContractBlock();

		if (items is null) return;
		foreach (var i in items)
			target.Give(i);
	}

	/// <summary>
	/// Receives items and iteratively adds them to the pool.<br/>
	/// WARNING: These items are considered 'dead' but resurrectable so be sure not to hold on to their reference.
	/// </summary>
	/// <param name="target">The pool to give to.</param>
	/// <param name="item1">The first item to give up to the pool.</param>
	/// <param name="item2">The second item to give up to the pool.</param>
	/// <param name="items">The remaining items to give up to the pool.</param>
	public static void Give<T>(this IObjectPool<T> target, T item1, T item2, params T[] items)
		where T : class
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		Contract.EndContractBlock();

		target.Give(item1);
		target.Give(item2);
		target.Give(items);
	}

	/// <summary>
	/// Provides a disposable RecycleHelper that contains an item from the pool.<br/>
	/// Will be returned to the pool once .Dispose() is called.
	/// </summary>
	/// <typeparam name="T">The object type from the pool.</typeparam>
	/// <param name="source">The object pool.</param>
	/// <returns>A RecycleHelper containing an item from the pool.</returns>
	public static RecycleHelper<T> Rent<T>(this IObjectPool<T> source)
		where T : class => new(source);

	/// <summary>
	/// Provides an item from the pool and returns it when the action sucessfully completes.
	/// </summary>
	/// <typeparam name="T">The object type from the pool.</typeparam>
	/// <param name="source">The object pool.</param>
	/// <param name="action">The action to execute.</param>
	public static void Rent<T>(this IObjectPool<T> source, Action<T> action)
		where T : class
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (action is null) throw new ArgumentNullException(nameof(action));
		Contract.EndContractBlock();

		var item = source.Take();
		action(item);
		source.Give(item);
	}

	/// <summary>
	/// Provides an item from the pool and returns it when the action sucessfully completes.
	/// </summary>
	/// <typeparam name="T">The object type from the pool.</typeparam>
	/// <param name="source">The object pool.</param>
	/// <param name="action">The action to execute.</param>
	/// <returns>The value from the action.</returns>
	public static TResult Rent<T, TResult>(this IObjectPool<T> source, Func<T, TResult> action)
		where T : class
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (action is null) throw new ArgumentNullException(nameof(action));
		Contract.EndContractBlock();

		var item = source.Take();
		var result = action(item);
		source.Give(item);
		return result;
	}

	/// <summary>
	/// Provides an item from the pool and returns it when the action sucessfully completes.
	/// </summary>
	/// <typeparam name="T">The object type from the pool.</typeparam>
	/// <param name="source">The object pool.</param>
	/// <param name="action">The action to execute.</param>
	public static async ValueTask RentAsync<T>(this IObjectPool<T> source, Func<T, ValueTask> action)
		where T : class
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (action is null) throw new ArgumentNullException(nameof(action));
		Contract.EndContractBlock();

		var item = source.Take();
		await action(item).ConfigureAwait(false);
		source.Give(item);
	}

	/// <summary>
	/// Provides an item from the pool and returns it when the action sucessfully completes.
	/// </summary>
	/// <typeparam name="T">The object type from the pool.</typeparam>
	/// <param name="source">The object pool.</param>
	/// <param name="action">The action to execute.</param>
	/// <returns>The value from the action.</returns>
	public static async ValueTask<TResult> RentAsync<T, TResult>(this IObjectPool<T> source, Func<T, ValueTask<TResult>> action)
		where T : class
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (action is null) throw new ArgumentNullException(nameof(action));
		Contract.EndContractBlock();

		var item = source.Take();
		var result = await action(item).ConfigureAwait(false);
		source.Give(item);
		return result;
	}
}
