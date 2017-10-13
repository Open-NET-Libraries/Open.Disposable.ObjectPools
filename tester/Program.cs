using Open.Collections;
using Open.Disposable;
using Open.Disposable.ObjectPools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Program
{

	static void ConsoleSeparator()
	{
		Console.WriteLine("------------------------------------");
	}

	static void ConsoleNewLine()
	{
		Console.WriteLine();
	}

	const int COUNT = 200;
	const int REPEAT = 5000;

	static TimedResult[] BenchmarkResults<T>(Func<IObjectPool<T>> poolFactory)
		where T : class
	{
		return Benchmark<T>.Results(COUNT, REPEAT, poolFactory);
	}

	static TimedResult[] BenchmarkResults<T>(uint count, uint repeat, Func<IObjectPool<T>> poolFactory)
	where T : class
	{
		return Benchmark<T>.Results(count, repeat, poolFactory);
	}

	static TimedResult[] BenchmarkResults<T>(uint repeat, Func<IObjectPool<T>> poolFactory)
	where T : class
	{
		return Benchmark<T>.Results(COUNT, repeat, poolFactory);
	}

	static void OutputResults<T>(Func<IObjectPool<T>> poolFactory)
		where T : class
	{
		var results = BenchmarkResults(poolFactory);
		foreach (var e in BenchmarkResults(poolFactory))
			Console.WriteLine(e);
		Console.WriteLine("{0} Total", results.Select(r => r.Duration).Aggregate((r1, r2) => r1 + r2));

		ConsoleNewLine();
	}

	static void Main(string[] args)
	{
		Console.Write("Initializing...");

		// Run once through first to scramble initial conditions.
		BenchmarkResults(100, () => ConcurrentBagObjectPool.Create<object>());
		BenchmarkResults(100, () => LinkedListObjectPool.Create<object>());
		BenchmarkResults(100, () => OptimisticArrayObjectPool.Create<object>());
		// BenchmarkResults(100, () => BufferBlockObjectPool.Create<object>());

		Console.SetCursorPosition(0, Console.CursorTop);

		Console.WriteLine("ConcurrentBagObjectPool.................................");
		OutputResults(() => ConcurrentBagObjectPool.Create<object>());

		Console.WriteLine("LinkedListObjectPool....................................");
		OutputResults(() => LinkedListObjectPool.Create<object>());

		Console.WriteLine("OptimisticArrayObjectPool...............................");
		OutputResults(() => OptimisticArrayObjectPool.Create<object>());

		//Console.WriteLine("BufferBlockObjectPool...................................");
		//OutputResults(() => BufferBlockObjectPool.Create<object>());

		Console.WriteLine("(press any key when finished)");
		Console.ReadKey();
	}


	static void OldTest()
	{
		var ts = new CancellationTokenSource();
		ts.Cancel();
		var task = Task.FromResult(ts.Token);


		var pool = BufferBlockObjectPool.Create(() => new object(), 1024);
		var trimmer = new ObjectPoolAutoTrimmer(20, pool);
		var clearer = new ObjectPoolAutoTrimmer(0, pool, TimeSpan.FromSeconds(5));
		var tank = new ConcurrentBag<object>();

		int count = 0;
		while (true)
		{
			Console.WriteLine("Before {0}: {1}, {2}", count, pool.Count, tank.Count);

			count++;
			tank.Add(pool.Take());
			count++;
			tank.Add(new object());
			count++;
			tank.Add(new object());
			foreach (var o in tank.TryTakeWhile(c => c.Count > 30))
				pool.Give(o);

			Console.WriteLine("After  {0}: {1}, {2}", count, pool.Count, tank.Count);

			if (count % 30 == 0)
			{
				Thread.Sleep(1000);
			}

			if (count % 40 == 0)
			{
				Console.WriteLine("-----------------");
				Thread.Sleep(11000);
			}

			Console.WriteLine();

		}

	}
}