using System;
using System.IO;
using System.Text;

namespace Open.Disposable.ObjectPools;

public class ConsoleReport<T>(TextWriter output = null)
	: Diagnostics.BenchmarkConsoleReport<Func<IObjectPool<T>>>(ITERATIONS, output, Benchmark<T>.Results)
	where T : class
{
	const int ITERATIONS = 100000;

	public ConsoleReport(StringBuilder output)
		: this(new StringWriter(output))
	{
	}
}
