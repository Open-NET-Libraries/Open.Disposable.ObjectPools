using Microsoft.Extensions.ObjectPool;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Open.Disposable.ObjectPools;

public class DefaultObjectPool<T>(Func<T> factory, int capacity = 64)
	: Microsoft.Extensions.ObjectPool.DefaultObjectPool<T>(new Policy(factory), capacity), IObjectPool<T>
	where T : class
{
	class Policy(Func<T> factory) : IPooledObjectPolicy<T>
	{
		private readonly Func<T> _factory = factory ?? throw new ArgumentNullException(nameof(factory));

		public T Create() => _factory();

		public bool Return(T obj) =>
			// recycler?.Invoke(obj);
			true;
	}

	public int Capacity { get; }

	public int Count => throw new NotImplementedException();

	[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
	public void Dispose()
	{
	}

	public T Generate() => factory();

	public void Give(T item) => Return(item);

	public T Take() => Get();

	public bool TryTake([NotNullWhen(true)] out T item)
	{
		item = Get();
		return true;
	}

	public T? TryTake() => Get();
}
