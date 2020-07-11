using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Open.Disposable
{
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
	public interface IReferenceContainer<T>
		where T : class
	{
		int Capacity { get; }
		bool SetIfNull(T value);
		bool TrySave(T value);
		T? TryRetrieve();
	}

	[DebuggerDisplay("{Value,nq}")]
	[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Undesired.")]
	public struct ReferenceContainer<T> : IReferenceContainer<T>
		where T : class
	{
		public int Capacity => 1;

		T? _value;

		public T? Value
		{
			get => _value;
			set => _value = value;
		}

		public bool SetIfNull(T value)
		{
			if (_value != null) return false;
			_value = value;
			return true;
		}

		public bool TrySave(T value)
			=> _value is null
				&& null == Interlocked.CompareExchange(ref _value, value, null);

		public T? TryRetrieve()
		{
			var item = _value;
			return (item != null
				&& item == Interlocked.CompareExchange(ref _value, null, item))
				? item
				: null;
		}
	}

	//public struct DualReferenceContainer<T> : IReferenceContainer<T>
	//	where T : class
	//{
	//	public int Capacity => 2;

	//	ReferenceContainer<T> _1;
	//	ReferenceContainer<T> _2;

	//	public bool SetIfNull(T value)
	//	{
	//		return _1.SetIfNull(value) || _2.SetIfNull(value);
	//	}

	//	public T TryRetrieve()
	//	{
	//		return _1.TryRetrieve() ?? _2.TryRetrieve();
	//	}

	//	public bool TrySave(T value)
	//	{
	//		return _1.TrySave(value) || _2.TrySave(value);
	//	}
	//}

}
