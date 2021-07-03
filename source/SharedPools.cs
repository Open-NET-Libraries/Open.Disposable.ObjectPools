﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Disposable
{
	public static class ListPool<T>
	{
		/// <summary>
		/// A shared object pool for use with lists.
		/// The list is cleared after being returned.
		/// The max size of the pool is 128 which should suffice for most use cases.
		/// </summary>
		public static readonly ConcurrentQueueObjectPool<List<T>> Shared = Create();

		/// <summary>
		/// Creates an object pool for use with lists.
		/// The list is cleared after being returned.
		/// </summary>
		public static ConcurrentQueueObjectPool<List<T>> Create(int capacity = Constants.DEFAULT_CAPACITY) => new(() => new List<T>(), h =>
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
		/// The max size of the pool is 128 which should suffice for most use cases.
		/// </summary>
		public static readonly ConcurrentQueueObjectPool<HashSet<T>> Shared = Create();

		/// <summary>
		/// Creates an object pool for use with hash-sets.
		/// The hash-set is cleared after being returned.
		/// </summary>
		public static ConcurrentQueueObjectPool<HashSet<T>> Create(int capacity = Constants.DEFAULT_CAPACITY) => new(() => new HashSet<T>(), h => h.Clear(), null, capacity);
	}

	public static class DictionaryPool<TKey, TValue>
	{
		/// <summary>
		/// A shared object pool for use with dictionaries.
		/// The dictionary is cleared after being returned.
		/// The max size of the pool is 128 which should suffice for most use cases.
		/// </summary>
		public static readonly ConcurrentQueueObjectPool<Dictionary<TKey, TValue>> Shared = Create();

		/// <summary>
		/// Creates an object pooll for use with dictionaries.
		/// The dictionary is cleared after being returned.
		/// </summary>
		public static ConcurrentQueueObjectPool<Dictionary<TKey, TValue>> Create(int capacity = Constants.DEFAULT_CAPACITY) => new(() => new Dictionary<TKey, TValue>(), h => h.Clear(), null, capacity);

	}
}