using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Disposable
{
	[TestClass]
	public class ObjectPoolBenchmarks
	{
		const int TEST_COUNT = 200;
		const int TEST_LOOPS = 1000;

		static void Benchmark<T>(IObjectPool<T> pool)
			where T : class
		{
			var tank = new ConcurrentBag<T>(); // This will have an effect on performance measurement, but hopefully consistently.
			int remaining = 0;
			using (pool)
			{
				Parallel.For(0, TEST_COUNT, i => tank.Add(pool.Take()));

				Parallel.ForEach(tank, e => pool.Give(e));

				while (pool.TryTake() != null) { remaining++; }
			}
			//Console.WriteLine("Remaining: {0}", remaining);
			Assert.IsTrue(remaining!=0);
		}

		static void Benchmark<T>(IEnumerable<IObjectPool<T>> pools)
			where T : class
		{
			foreach (var p in pools)
				Benchmark(p);
		}

		OptimisticArrayObjectPool<object>[] OptimisticArrayObjectPools
			= Enumerable.Range(0, TEST_LOOPS)
			.Select(i => OptimisticArrayObjectPool.Create<object>(TEST_COUNT * 2))
			.ToArray();

		[TestMethod]
		public void OptimisticArrayObjectPool_Benchmark()
		{
			Benchmark(OptimisticArrayObjectPools);
		}

		ConcurrentBagObjectPool<object>[] ConcurrentBagObjectPools
			= Enumerable.Range(0, TEST_LOOPS)
			.Select(i => ConcurrentBagObjectPool.Create<object>(TEST_COUNT * 2))
			.ToArray();

		[TestMethod]
		public void ConcurrentBagObjectPool_Benchmark()
		{
			Benchmark(ConcurrentBagObjectPools);
		}

		BufferBlockObjectPool<object>[] BufferBlockObjectPools
			= Enumerable.Range(0, TEST_LOOPS)
			.Select(i => BufferBlockObjectPool.Create<object>(TEST_COUNT * 2))
			.ToArray();

		[TestMethod]
		public void BufferBlockObjectPool_Benchmark()
		{
			Benchmark(BufferBlockObjectPools);
		}


		LinkedListObjectPool<object>[] LinkedListObjectPools
			= Enumerable.Range(0, TEST_LOOPS)
			.Select(i => LinkedListObjectPool.Create<object>(TEST_COUNT * 2))
			.ToArray();

		[TestMethod]
		public void LinkedListObjectPool_Benchmark()
		{
			Benchmark(LinkedListObjectPools);
		}
	}
}
