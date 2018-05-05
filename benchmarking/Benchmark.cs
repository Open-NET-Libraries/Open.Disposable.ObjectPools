using Open.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open.Disposable.ObjectPools
{
	public class Benchmark<T> : BenchmarkBase<Func<IObjectPool<T>>>
		where T : class
	{
		public Benchmark(uint size, uint repeat, Func<IObjectPool<T>> poolFactory) : base(size, repeat, poolFactory)
		{
			// Because some pools do comparison checks on values, we have have unique instances/values.
			_items = new T[(int)size];
		}

		readonly T[] _items;

		protected override IEnumerable<TimedResult> TestOnceInternal()
		{
			using (var pool = Param())
			{

				yield return TimedResult.Measure("Take From Empty (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i => _items[i] = pool.Take());
				});

				yield return TimedResult.Measure("Give To (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i => pool.Give(_items[i]));
				});

				yield return TimedResult.Measure("Mixed Read/Write (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i =>
					{
						if (i % 2 == 0)
							_items[i] = pool.Take();
						else
							pool.Give(_items[i]);
					});
				});

				yield return TimedResult.Measure("Empty Pool (.TryTake())", () =>
				{
					while (pool.TryTake() != null)
					{
						// remaining++;
					}
				});
			}
		}

		public static TimedResult[] Results(uint size, uint repeat, Func<IObjectPool<T>> poolFactory)
		{
			return (new Benchmark<T>(size, repeat, poolFactory)).Result;
		}

	}
}
