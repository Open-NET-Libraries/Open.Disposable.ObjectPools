using Open.Collections;
using Open.Disposable;
using Open.Disposable.ObjectPools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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

	const int ITERATIONS = 100000;

	static TimedResult[] BenchmarkResults<T>(uint count, uint repeat, Func<IObjectPool<T>> poolFactory)
	where T : class
	{
		return Benchmark<T>.Results(count, repeat, poolFactory);
	}

	static readonly Regex TimeSpanRegex = new Regex(@"((?:00:)+ (?:0\B)?) ([0.]*) (\S*)", RegexOptions.IgnorePatternWhitespace);

	static void OutputResult(TimeSpan result)
	{
		var match = TimeSpanRegex.Match(result.ToString());
		Console.ForegroundColor = ConsoleColor.Black;
		Console.Write(match.Groups[1].Value);
		Console.ForegroundColor = ConsoleColor.DarkGray;
		Console.Write(match.Groups[2].Value);
		Console.ResetColor();
		Console.Write(match.Groups[3].Value);
	}

	static void OutputResult(TimedResult result, ConsoleColor? labelColor = null)
	{
		OutputResult(result.Duration);
		Console.Write(' ');
		if (labelColor.HasValue) Console.ForegroundColor = labelColor.Value;
		Console.WriteLine(result.Label.Substring(4));
		Console.ResetColor();
	}

	static void OutputResult(TimedResult result, TimedResult[] all)
	{
		var duration = result.Duration;
		OutputResult(duration);
		Console.Write(' ');
		var these = all.Where(r => r.Label == result.Label).Select(r=>r.Duration).OrderBy(d=>d).ToArray();
		var min = these.First();
		var max = these.Last();
		if(min!=max)
		{
			if (duration == min)
				Console.ForegroundColor = ConsoleColor.Green;
			else if (duration == max)
				Console.ForegroundColor = ConsoleColor.Red;
		}
		Console.WriteLine(result.Label.Substring(4));
		Console.ResetColor();
	}

	static void OutputResults(IEnumerable<TimedResult> results)
	{
		foreach (var e in results)
			OutputResult(e);

		ConsoleNewLine();
	}

	static void OutputResults(IEnumerable<TimedResult> results, TimedResult[] all)
	{
		foreach (var e in results)
			OutputResult(e, all);

		ConsoleNewLine();
	}

	static TimedResult[] OutputResults<T>(uint count, uint repeat, Func<IObjectPool<T>> poolFactory)
		where T : class
	{
		var results = BenchmarkResults(count, repeat, poolFactory);
		OutputResults(results);
		return results;
	}

	static void Test(uint count, uint multiple = 1)
	{
		var repeat = multiple * ITERATIONS / count;
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine("Repeat {1:g} for size {0:g}", count, repeat);
		ConsoleSeparator();
		Console.ResetColor();
		ConsoleNewLine();

		var cursor = Console.CursorTop;
		var results = new List<Tuple<string,TimedResult[]>>(3);

		string header;
		var capacity = (int)count * 2;

		Console.WriteLine(header = "LinkedListObjectPool....................................");
		results.Add(Tuple.Create(header, OutputResults(count, repeat, () => LinkedListObjectPool.Create<object>(capacity))));

		Console.WriteLine(header = "QueueObjectPool.........................................");
		results.Add(Tuple.Create(header, OutputResults(count, repeat, () => QueueObjectPool.Create<object>(capacity))));

		Console.WriteLine(header = "OptimisticArrayObjectPool...............................");
		results.Add(Tuple.Create(header, OutputResults(count, repeat, () => OptimisticArrayObjectPool.Create<object>(capacity))));

		Console.WriteLine(header = "InterlockedArrayObjectPool..............................");
		results.Add(Tuple.Create(header, OutputResults(count, repeat, () => InterlockedArrayObjectPool.Create<object>(capacity))));

		var all = results.SelectMany(r => r.Item2).ToArray();

		Console.SetCursorPosition(0, cursor);

		foreach(var r in results)
		{
			Console.WriteLine(r.Item1);
			OutputResults(r.Item2, all);
		}

		ConsoleNewLine();
	}

	static void Main(string[] args)
	{
		Console.Write("Initializing...");

		// Run once through first to scramble/warm-up initial conditions.
		BenchmarkResults(100, 100, () => LinkedListObjectPool.Create<object>(200));
		BenchmarkResults(100, 100, () => QueueObjectPool.Create<object>(200));
		BenchmarkResults(100, 100, () => OptimisticArrayObjectPool.Create<object>(200));
		BenchmarkResults(100, 100, () => InterlockedArrayObjectPool.Create<object>(200));
		// BenchmarkResults(100, 100, () => BufferBlockObjectPool.Create<object>());

		Console.SetCursorPosition(0, Console.CursorTop);

		Test(4);
		Test(10, 2);
		Test(100, 8);
		Test(250, 16);
		Test(1000, 24);
		Test(2000, 32);
		Test(4000, 48);


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