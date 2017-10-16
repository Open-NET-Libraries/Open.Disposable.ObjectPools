using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Disposable.ObjectPools
{
	public class Report //<T>
		//where T : class
    {
		TextWriter Output;
		public Report(TextWriter output = null)
		{
			Output = output;

			// Run once through first to scramble/warm-up initial conditions.
			BenchmarkResults(100, 100, () => LinkedListObjectPool.Create<object>(200));
			BenchmarkResults(100, 100, () => QueueObjectPool.Create<object>(200));
			BenchmarkResults(100, 100, () => OptimisticArrayObjectPool.Create<object>(200));
			BenchmarkResults(100, 100, () => InterlockedArrayObjectPool.Create<object>(200));
			// BenchmarkResults(100, 100, () => BufferBlockObjectPool.Create<object>());
		}

		string[] _resultLabels;
		List<string[]> _results = new List<string[]>();
		public string[][] Results => _results.ToArray();
		public string[] ResultLabels => _resultLabels.ToArray();

		public Report(StringBuilder output) : this (new StringWriter(output))
		{
		}

		//List<Func<int, IObjectPool<T>>> PoolFactories;

		const string SEPARATOR = "------------------------------------";
		public void Separator(bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine(SEPARATOR);
			Console.WriteLine(SEPARATOR);
		}

		public void NewLine(bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine();
			Console.WriteLine();
		}

		public void Write(char value, bool consoleOnly = false)
		{
			if(!consoleOnly) Output?.Write(value);
			Console.Write(value);
		}

		public void Write(string value, bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.Write(value);
			Console.Write(value);
		}

		public void WriteLine(string value, bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine(value);
			Console.WriteLine(value);
		}

		public static TimedResult[] BenchmarkResults<T>(uint count, uint repeat, Func<IObjectPool<T>> poolFactory)
		where T : class
		{
			return Benchmark<T>.Results(count, repeat, poolFactory);
		}

		static readonly Regex TimeSpanRegex = new Regex(@"((?:00:)+ (?:0\B)?) ([0.]*) (\S*)", RegexOptions.IgnorePatternWhitespace);

		void OutputResult(TimeSpan result, bool consoleOnly = false)
		{
			var match = TimeSpanRegex.Match(result.ToString());
			Console.ForegroundColor = ConsoleColor.Black;
			Write(match.Groups[1].Value, consoleOnly);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Write(match.Groups[2].Value, consoleOnly);
			Console.ResetColor();
			Write(match.Groups[3].Value, consoleOnly);
		}

		void OutputResult(TimedResult result, ConsoleColor? labelColor = null, bool consoleOnly = false)
		{
			OutputResult(result.Duration, consoleOnly);
			Write(' ', consoleOnly);
			if (labelColor.HasValue) Console.ForegroundColor = labelColor.Value;
			WriteLine(result.Label.Substring(4), consoleOnly);
			Console.ResetColor();
		}

		void OutputResult(TimedResult result, TimedResult[] all, bool consoleOnly = false)
		{
			var duration = result.Duration;
			OutputResult(duration, consoleOnly);
			Write(' ', consoleOnly);
			var these = all.Where(r => r.Label == result.Label).Select(r => r.Duration).OrderBy(d => d).ToArray();
			var min = these.First();
			var max = these.Last();
			if (min != max)
			{
				if (duration == min)
					Console.ForegroundColor = ConsoleColor.Green;
				else if (duration == max)
					Console.ForegroundColor = ConsoleColor.Red;
			}
			WriteLine(result.Label.Substring(4), consoleOnly);
			Console.ResetColor();
		}

		void OutputResults(IEnumerable<TimedResult> results, bool consoleOnly = false)
		{
			foreach (var e in results)
				OutputResult(e, consoleOnly: consoleOnly);

			NewLine(consoleOnly);
		}

		void OutputResults(IEnumerable<TimedResult> results, TimedResult[] all, bool consoleOnly = false)
		{
			foreach (var e in results)
				OutputResult(e, all, consoleOnly);

			NewLine(consoleOnly);
		}

		TimedResult[] OutputResults<T>(uint count, uint repeat, Func<IObjectPool<T>> poolFactory, bool consoleOnly = false)
			where T : class
		{
			var results = BenchmarkResults(count, repeat, poolFactory);
			OutputResults(results, consoleOnly);
			return results;
		}

		const int ITERATIONS = 100000;

		public Tuple<string, TimedResult[]> TestResult(string batch, string poolName, uint count, uint repeat, Func<IObjectPool<object>> factory)
		{
			var header = poolName + "........................................................".Substring(poolName.Length);
			WriteLine(header);
			var results = OutputResults(count, repeat, factory);

			if(_resultLabels==null)
			{
				var labels = new List<string>
				{
					"Batch",
					"Pool Type",
				};
				foreach (var r in results)
					labels.Add(r.Label.Substring(4));
				_resultLabels = labels.ToArray();
			}

			var list = new List<string>
			{
				batch,
				poolName
			};
			foreach (var r in results)
				list.Add(r.Duration.ToString());
			_results.Add(list.ToArray());

			return Tuple.Create(header, results);
		}

		public void Test(uint count, uint multiple = 1)
		{
			var data = new List<string>();
			var repeat = multiple * ITERATIONS / count;
			Console.ForegroundColor = ConsoleColor.Cyan;
			var batch = String.Format("Repeat {1:g} for size {0:g}", count, repeat);
			WriteLine(batch);
			Separator();
			Console.ResetColor();
			NewLine();

			var cursor = Console.CursorTop;
			var results = new List<Tuple<string, TimedResult[]>>(3);

			var capacity = (int)count * 2;

			results.Add(TestResult(batch, "LinkedListObjectPool", count, repeat,  () => LinkedListObjectPool.Create<object>(capacity)));
			results.Add(TestResult(batch, "QueueObjectPool", count, repeat, () => QueueObjectPool.Create<object>(capacity)));
			results.Add(TestResult(batch, "OptimisticArrayObjectPool", count, repeat, () => OptimisticArrayObjectPool.Create<object>(capacity)));
			results.Add(TestResult(batch, "InterlockedArrayObjectPool", count, repeat, () => InterlockedArrayObjectPool.Create<object>(capacity)));

			var all = results.SelectMany(r => r.Item2).ToArray();

			Console.SetCursorPosition(0, cursor);

			foreach (var r in results)
			{
				Console.WriteLine(r.Item1);
				OutputResults(r.Item2, all, true);
			}

			NewLine();
		}
	}
}
