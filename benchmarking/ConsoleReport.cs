using System;
using System.IO;
using System.Text;

namespace Open.Disposable.ObjectPools
{
	public class ConsoleReport<T> : Diagnostics.BenchmarkConsoleReport<Func<IObjectPool<T>>>
		where T : class
	{
		const int ITERATIONS = 100000;
		public ConsoleReport(TextWriter output = null) : base(ITERATIONS, output, Benchmark<T>.Results)
		{
		}

		public ConsoleReport(StringBuilder output) : this(new StringWriter(output))
		{
		}
	}
}
