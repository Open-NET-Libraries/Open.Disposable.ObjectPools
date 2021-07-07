using Open.Disposable;
using Open.Disposable.ObjectPools;
using Open.Text.CSV;
using System;
using System.IO;
using System.Text;

class Program
{
	static void Main()
	{
		Console.Write("Initializing...");

		var sb = new StringBuilder();
		var report = new ConsoleReport<object>(sb);

		/*
		 * Notes:
		 * 1) ConcurrentBag seems to be PAINFULLY slow compared to ConcurrentQueue.
		 * 2) The speed gap between an optimistic array and its interlocked couterpart grows large very quickly (as expected).
		 * 3) A sync locked Queue is faster than a sync locked LinkedList.
		 * 4) ConcurrentQueue seems to be the overall winner when dealing with pools larger than 100 but is the clear loser for very small sizes.
		 */

		// Start with a baseline...
		//report.AddBenchmark("QueueObjectPool", // Note, that this one isn't far off from the following in peformance.
		//	count => () => QueueObjectPool.Create<object>((int)count * 2));

		report.AddBenchmark("ConcurrentQueueObjectPool", // Note, that this one isn't far off from the following in peformance, but definitely is faster than LinkedListObjectPool and the rest.
			count => () => ConcurrentQueueObjectPool.Create<object>((int)count * 2));

		//report.AddBenchmark("ConcurrentStackObjectPool", // Note, that this one isn't far off from the following in peformance, but definitely is faster than LinkedListObjectPool and the rest.
		//	count => () => ConcurrentStackObjectPool.Create<object>((int)count * 2));

		report.AddBenchmark("OptimisticArrayObjectPool",
			count => () => OptimisticArrayObjectPool.Create<object>((int)count * 2));

		// Is ineveitably slower than the above but should be enabled for testing code changes.
		report.AddBenchmark("InterlockedArrayObjectPool",
			count => () => InterlockedArrayObjectPool.Create<object>((int)count * 2));

		report.Pretest(200, 200); // Run once through first to scramble/warm-up initial conditions.

		Console.SetCursorPosition(0, Console.CursorTop);

		const int loopMultiple = 6;
		report.Test(4, 8 * loopMultiple);
		report.Test(10, 8 * loopMultiple);
		report.Test(50, 12 * loopMultiple);
		report.Test(100, 16 * loopMultiple);
		report.Test(250, 32 * loopMultiple);
		//report.Test(2000, 64 * loopMultiple);

		File.WriteAllText("./BenchmarkResult.txt", sb.ToString());
		using (var fs = File.OpenWrite("./BenchmarkResult.csv"))
		using (var sw = new StreamWriter(fs))
		using (var csv = new CsvWriter(sw))
		{
			csv.WriteRow(report.ResultLabels);
			csv.WriteRows(report.Results);
		}

		Console.Beep();
		Console.WriteLine("(press any key when finished)");
		Console.ReadKey();
	}

}
