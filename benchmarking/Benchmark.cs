using Open.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if DEBUG
using System.Diagnostics;
#endif

namespace Open.Disposable.ObjectPools
{
	public class Benchmark<T> : BenchmarkBase<Func<IObjectPool<T>>>
		where T : class
	{
		public Benchmark(uint size, uint repeat, Func<IObjectPool<T>> poolFactory) : base(size, repeat, poolFactory)
		{
			// Because some pools do comparison checks on values, we have have unique instances/values.
			_items = new T[(int)size];
			var pool = Param();
			Parallel.For(0, TestSize, i => _items[i] = pool.Take());
		}

		readonly T[] _items;

		protected override IEnumerable<TimedResult> TestOnceInternal()
		{
			using var pool = Param();
			if (pool is null) throw new NullReferenceException();
			//yield return TimedResult.Measure("Take From Empty (In Parallel)", () =>
			//{
			//	Parallel.For(0, TestSize, i => _items[i] = pool.Take());
			//});

			yield return TimedResult.Measure("Give To (In Parallel)", () =>
			{
				// ReSharper disable once AccessToDisposedClosure
				Parallel.For(0, TestSize, i => pool.Give(_items[i]));
#if DEBUG
				if (pool is DefaultObjectPool<object>) return;
				var count = pool.Count;
				Debug.Assert(pool is OptimisticArrayObjectPool<T> || count == TestSize, $"Expected {TestSize}, acutal count: {count}");
#endif
			});

			yield return TimedResult.Measure("Mixed 90%-Take/10%-Give (In Parallel)", () =>
			{
				Parallel.For(0, TestSize, i =>
				{
					if (i % 10 == 0)
						pool.Give(_items[i]);
					else
						_items[i] = pool.Take();
				});
			});

			yield return TimedResult.Measure("Mixed 50%-Take/50%-Give (In Parallel)", () =>
			{
				Parallel.For(0, TestSize, i =>
				{
					if (i % 2 == 0)
						_items[i] = pool.Take();
					else
						pool.Give(_items[i]);
				});
			});

			yield return TimedResult.Measure("Mixed 10%-Take/90%-Give (In Parallel)", () =>
			{
				Parallel.For(0, TestSize, i =>
				{
					if (i % 10 == 0)
						_items[i] = pool.Take();
					else
						pool.Give(_items[i]);
				});
			});

			//if (pool is DefaultObjectPool<object>) yield break;
			//yield return TimedResult.Measure("Empty Pool (.TryTake())", () =>
			//{

			//	while (pool.TryTake() is not null)
			//	{
			//		// remaining++;
			//	}
			//});
		}

		public static TimedResult[] Results(uint size, uint repeat, Func<IObjectPool<T>> poolFactory)
		{
			return (new Benchmark<T>(size, repeat, poolFactory)).Result;
		}

	}
}
