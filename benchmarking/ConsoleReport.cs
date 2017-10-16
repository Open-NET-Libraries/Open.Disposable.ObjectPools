using System;
using System.IO;
using System.Text;

namespace Open.Disposable.ObjectPools
{
	public class ConsoleReport<T> : Open.Diagnostics.BenchmarkConsoleReport<Func<IObjectPool<T>>>
		where T : class
    {
		const int ITERATIONS = 100000;
		public ConsoleReport(TextWriter output = null) : base(ITERATIONS, output, (c,r,p)=> Benchmark<T>.Results(c, r, p))
		{
		}

		public ConsoleReport(StringBuilder output) : this (new StringWriter(output))
		{
		}	
	}
}
