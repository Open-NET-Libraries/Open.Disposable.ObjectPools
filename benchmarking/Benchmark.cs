using Open.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Open.Disposable.ObjectPools
{
	public class Benchmark<T> : BenchmarkBase<Func<IObjectPool<T>>>
		where T : class
	{
		public Benchmark(uint size, uint repeat, Func<IObjectPool<T>> poolFactory) : base(size,repeat,poolFactory)
		{
		}


		protected override IEnumerable<TimedResult> TestOnceInternal()
		{
			var disposeTimer = new Stopwatch();
			using (var pool = TimedResult.Measure(out TimedResult constructionTime, "Pool Construction", Param))
			{
				// yield return constructionTime; // Looking for anomalies.

				// Indicates how long pure construction takes.  The baseline by which you should measure .Take()
				//yield return TimedResult.Measure("02) Pool.Generate() (In Parallel)", () =>
				//{
				//	Parallel.For(0, TestSize, i => pool.Generate());
				//});

				var tank = new ConcurrentBag<T>(); // This will have an effect on performance measurement, but hopefully consistently.

				yield return TimedResult.Measure("Take From Empty (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i => tank.Add(pool.Take()));
				});

				// Put extra in the tank.
				for (var i = 0; i < TestSize; i++)
				{
					tank.Add(pool.Generate());
				}

				yield return TimedResult.Measure("Give To (In Parallel)", () =>
				{
					Parallel.ForEach(tank, e => pool.Give(e));
				});

				yield return TimedResult.Measure("Mixed Read/Write (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i =>
					{
						if (i % 2 == 0)
							tank.Add(pool.Take());
						else if (tank.TryTake(out T value))
							pool.Give(value);
					});
				});

				yield return TimedResult.Measure("Empty Pool (.TryTake())", () =>
				{
					while (pool.TryTake() != null) {
						// remaining++;
					}
				});

				disposeTimer.Start();
			}
			disposeTimer.Stop();

			// yield return new TimedResult("98) Pool Disposal", disposeTimer);  // Looking for anomalies.
		}

		public static TimedResult[] Results(uint size, uint repeat, Func<IObjectPool<T>> poolFactory)
		{
			return (new Benchmark<T>(size, repeat, poolFactory)).Result;
		}

	}
}
