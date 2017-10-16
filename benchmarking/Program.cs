using Open.Collections;
using Open.Disposable;
using Open.Disposable.ObjectPools;
using Open.Text.CSV;
using Open.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
	static void Main(string[] args)
	{
		Console.Write("Initializing...");

		var sb = new StringBuilder();
		var report = new Report(sb);
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