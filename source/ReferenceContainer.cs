using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Open.Disposable;

public interface IReferenceContainer<T>
	where T : class
{
	int Capacity { get; }
	bool SetIfNull(T value);
	bool TrySave(T value);
	T? TryRetrieve();
}

[DebuggerDisplay("{Value,nq}")]
public struct ReferenceContainer<T> : IReferenceContainer<T>
	where T : class
{
	public int Capacity => 1;

	T? _value;

	/// <summary>
	/// The value contained.
	/// </summary>
	public T? Value
	{
		get => _value;
		set => _value = value;
	}

	/// <summary>
	/// Sets the value if it is currently null without interlocking.
	/// </summary>
	/// <returns>true if the value was set; otherwise false.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool SetIfNull(T value)
	{
		if (_value is not null) return false;
		_value = value;
		return true;
	}

	/// <summary>
	/// Tries to atomically store the value.
	/// </summary>
	/// <returns>true if the value was set; otherwise false.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TrySave(T value)
		=> _value is null && null == Interlocked.CompareExchange(ref _value, value, null);

	/// <summary>
	/// Tries to atomically retrieve the value.
	/// </summary>
	/// <returns>The value retrieved; otherwise null.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T? TryRetrieve()
	{
		var item = _value;
		return (item is not null
			&& item == Interlocked.CompareExchange(ref _value, null, item))
			? item
			: null;
	}
}
