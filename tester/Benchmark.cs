using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Disposable.ObjectPools
{
    public class Benchmark<T>
		where T : class
	{
		public readonly uint TestSize; // Pool capacity should be at least 2x this.
		public readonly uint RepeatCount; // Number of times to repeat the test from scratch.
		public readonly Func<IObjectPool<T>> PoolFactory;

		public Benchmark(uint size, uint repeat, Func<IObjectPool<T>> poolFactory)
		{
			TestSize = size;
			RepeatCount = repeat;
			PoolFactory = poolFactory;
		}

		public IEnumerable<TimedResult> TestOnce()
		{
			var total = TimeSpan.Zero;
			foreach(var r in TestOnceInternal())
			{
				total += r.Duration;
				yield return r;
			}

			yield return new TimedResult("99) TOTAL", total);
		}

		IEnumerable<TimedResult> TestOnceInternal()
		{
			var disposeTimer = new Stopwatch();
			using (var pool = TimedResult.Measure(out TimedResult constructionTime, "01) Pool Construction", PoolFactory))
			{
				yield return constructionTime;

				// Indicates how long pure construction takes.  The baseline by which you should measure .Take()
				yield return TimedResult.Measure("02) Pool.Generate() (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i => pool.Generate());
				});

				var tank = new ConcurrentBag<T>(); // This will have an effect on performance measurement, but hopefully consistently.
												   //int remaining = 0;

				yield return TimedResult.Measure("03) Take From Empty (In Parallel)", () =>
				{
					Parallel.For(0, TestSize, i => tank.Add(pool.Take()));
				});

				yield return TimedResult.Measure("04) Give To (In Parallel)", () =>
				{
					Parallel.ForEach(tank, e => pool.Give(e));
				});

				yield return TimedResult.Measure("05) Empty Pool (.TryTake())", () =>
				{
					while (pool.TryTake() != null) {
						// remaining++;
					}
				});

				disposeTimer.Start();
			}
			disposeTimer.Stop();

			yield return new TimedResult("98) Pool Disposal", disposeTimer);
		}

		public IEnumerable<IEnumerable<TimedResult>> TestRepeated()
		{
			for(var i=0;i<RepeatCount;i++)
			{
				yield return TestOnce();
			}
		}


		TimedResult[] _result;
		public TimedResult[] Result
		{
			get
			{
				return LazyInitializer.EnsureInitialized(ref _result, ()=>
					TestRepeated()
					.SelectMany(s => s)
					.GroupBy(k => k.Label)
					.Select(g => g.Sum())
					.OrderBy(r => r.Label)
					.ToArray()
				);
			}
		}

		public static TimedResult[] Results(uint size, uint repeat, Func<IObjectPool<T>> poolFactory)
		{
			return (new Benchmark<T>(size, repeat, poolFactory)).Result;
		}

	}
}
