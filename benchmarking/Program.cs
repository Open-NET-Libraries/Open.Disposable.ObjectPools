using Open.Disposable;
using Open.Disposable.ObjectPools;
using Open.Text.CSV;
using System;
using System.IO;
using System.Text;

class Program
{
	static void Main(string[] args)
	{
		Console.Write("Initializing...");

		var sb = new StringBuilder();
		var report = new ConsoleReport<object>(sb);

		report.AddBenchmark("QueueObjectPool", // Note, that this one isn't far off from the following in peformance, but definitely is faster than LinkedListObjectPool and the rest.
			count => () => QueueObjectPool.Create<object>((int)count * 2));
		report.AddBenchmark("OptimisticArrayObjectPool",
			count => () => OptimisticArrayObjectPool.Create<object>((int)count * 2));
		report.AddBenchmark("InterlockedArrayObjectPool",
			count => () => InterlockedArrayObjectPool.Create<object>((int)count * 2));
		report.Pretest(200, 200); // Run once through first to scramble/warm-up initial conditions.

		Console.SetCursorPosition(0, Console.CursorTop);

		report.Test(4, 4);
		report.Test(10, 4);
		report.Test(100, 8);
		report.Test(250, 16);
		report.Test(1000, 24);
		report.Test(2000, 32);
		report.Test(4000, 48);

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