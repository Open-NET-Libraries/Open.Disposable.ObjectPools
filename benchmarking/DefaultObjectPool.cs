using Microsoft.Extensions.ObjectPool;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Open.Disposable.ObjectPools
{
	public class DefaultObjectPool<T> : Microsoft.Extensions.ObjectPool.DefaultObjectPool<T>, IObjectPool<T>
		where T : class
	{
		private readonly Func<T> _factory;

		class Policy : IPooledObjectPolicy<T>
		{
			private readonly Func<T> factory;
			//			private readonly Action<T>? recycler;

			public Policy(Func<T> factory)//, Action<T>? recycler)
			{
				if (factory is null)
					throw new ArgumentNullException(nameof(factory));


				this.factory = factory;
				//this.recycler = recycler;
			}
			public T Create() => factory();

			public bool Return(T obj) =>
				// recycler?.Invoke(obj);
				true;
		}

		public DefaultObjectPool(Func<T> factory, int capacity = 64)
			: base(new Policy(factory), capacity) => _factory = factory;

		public int Capacity { get; }

		public int Count => throw new NotImplementedException();

		public void Dispose()
		{
		}

		public T Generate() => _factory();

		public void Give(T item) => Return(item);

		public T Take() => Get();

		public bool TryTake([NotNullWhen(true)] out T item)
		{
			item = Get();
			return true;
		}

		public T? TryTake() => Get();
	}
}
