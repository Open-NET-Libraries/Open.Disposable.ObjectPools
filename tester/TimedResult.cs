using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Open.Disposable
{
	public struct TimedResult : IComparable<TimedResult>, IEquatable<TimedResult>
	{
		public TimedResult(string label, TimeSpan duration)
		{
			Label = label;
			Duration = duration;
		}

		public TimedResult(string label, Stopwatch stopwatch) : this(label, stopwatch.Elapsed)
		{

		}
		public readonly string Label;
		public readonly TimeSpan Duration;

		public override string ToString()
		{
			return String.Format("{1} {0}", Label, Duration);
		}

		public static TimeSpan Measure(Action action)
		{
			var sw = Stopwatch.StartNew();
			action();
			sw.Stop();
			return sw.Elapsed;
		}

		public static TimedResult Measure(string label, Action action)
		{
			return new TimedResult(label, Measure(action));
		}

		public static T Measure<T>(out TimeSpan duration, Func<T> action)
		{
			var sw = Stopwatch.StartNew();
			var result = action();
			sw.Stop();
			duration = sw.Elapsed;
			return result;
		}

		public static T Measure<T>(out TimedResult measurement, string label, Func<T> action)
		{
			var result = Measure(out TimeSpan duration, action);
			measurement = new TimedResult(label, duration);
			return result;
		}

		public bool Equals(TimedResult other)
		{
			return base.Equals(other) || Label == other.Label && Duration == other.Duration;
		}

		public int CompareTo(TimedResult other)
		{
			if (Label != other.Label) throw new InvalidOperationException("Comparing two timed results that are not the same label.");
			if (Duration < other.Duration) return -1;
			if (Duration > other.Duration) return +1;
			return 0;
		}

		public static bool operator <(TimedResult tr1, TimedResult tr2)
		{
			return tr1.CompareTo(tr2) < 0;
		}

		public static bool operator >(TimedResult tr1, TimedResult tr2)
		{
			return tr1.CompareTo(tr2) > 0;
		}

		public static bool operator <=(TimedResult tr1, TimedResult tr2)
		{
			return tr1.CompareTo(tr2) <= 0;
		}

		public static bool operator >=(TimedResult tr1, TimedResult tr2)
		{
			return tr1.CompareTo(tr2) >= 0;
		}

		public static TimedResult operator +(TimedResult tr1, TimedResult tr2)
		{
			var label = tr1.Label;
			if (label != tr2.Label) throw new InvalidOperationException("Adding two timed results that are not the same label.");
			return new TimedResult(label, tr1.Duration + tr2.Duration);
		}

		public static TimedResult operator -(TimedResult tr1, TimedResult tr2)
		{
			var label = tr1.Label;
			if (label != tr2.Label) throw new InvalidOperationException("Adding two timed results that are not the same label.");
			return new TimedResult(label, tr1.Duration - tr2.Duration);
		}

	}

	public static class TimedResultExtensions
	{
		public static TimedResult Sum(this IEnumerable<TimedResult> results)
		{
			return results.Aggregate((a, b) => a + b);
		}
	}

}
