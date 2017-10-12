using Open.Collections;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
		var ts = new CancellationTokenSource();
		ts.Cancel();
		var task = Task.FromResult(ts.Token);


        var pool = new Open.Disposable.ObjectPool<object>(20, () => new object(), null, TimeSpan.FromSeconds(5) );
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

            Console.WriteLine("After  {0}: {1}, {2}",count, pool.Count, tank.Count);

			if (count % 30 == 0)
			{
				Console.WriteLine("Trimmed: {0}", pool.Trim().Result);
			}

			if (count % 40 == 0)
			{
				Console.WriteLine("-----------------");
			}

			Console.WriteLine();

		}
	}
}
