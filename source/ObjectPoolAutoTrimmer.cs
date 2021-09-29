using Open.Threading.Tasks;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Open.Disposable;

[SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Micro-optimization for retrieving this value as read-only is slightly slower")]
public class ObjectPoolAutoTrimmer
	: DisposableBase
{
	ITrimmableObjectPool _pool;
	ActionRunner _trimmer;

	// Micro-optimization for retrieving this value as read-only is slightly slower.
	ushort _trimmedSize;
	TimeSpan _trimDelay;

	/// <summary>
	///  Max size that trimming will allow.
	/// </summary>
	// ReSharper disable once NotAccessedField.Global
	public readonly ushort TrimmedSize;

	/// <summary>
	/// Time to wait/defer trimming. Default is 500 milliSeconds.
	/// </summary>
	// ReSharper disable once NotAccessedField.Global
	public readonly TimeSpan TrimDelay;

	/// <summary>
	/// Constructs an auto-trimming ObjectPool helper.
	/// </summary>
	/// <param name="pool">The governable object pool to maintain.</param>
	/// <param name="trimmedSize">The target size to limit to after a half second timeout.  Allowing the pool to still grow to the max size until the trim occurs.</param>
	/// <param name="trimDelay">The amount of time to wait/defer trimming.</param>
	public ObjectPoolAutoTrimmer(
		ushort trimmedSize,
		ITrimmableObjectPool pool,
		TimeSpan? trimDelay = null)
	{
		_pool = pool ?? throw new ArgumentNullException(nameof(pool));

		if (pool is DisposableBase d)
		{
			if (d.WasDisposed) throw new ArgumentException("Cannot trim for an object pool that is already disposed.");
			d.BeforeDispose += Pool_BeforeDispose;
		}

		TrimmedSize = _trimmedSize = trimmedSize;
		TrimDelay = _trimDelay = trimDelay ?? TimeSpan.FromMilliseconds(500);

		_trimmer = new ActionRunner(TrimInternal);

		pool.Received += Target_GivenTo;
		pool.Released += Target_TakenFrom;
	}

	protected virtual void Target_GivenTo(int newSize)
	{
		if (newSize >= 0 && newSize > _trimmedSize)
			_trimmer?.Defer(_trimDelay, false);
	}

	protected virtual void Target_TakenFrom(int newSize)
	{
		if (newSize >= 0 && newSize <= _trimmedSize)
			_trimmer?.Cancel();
	}

	void Pool_BeforeDispose(object sender, EventArgs e) => Dispose();

	protected virtual void TrimInternal()
	{
		_trimmer?.Cancel();
		_pool?.TrimTo(_trimmedSize);
	}

	protected override void OnDispose()
	{
		DisposeOf(ref _trimmer);

		var target = Nullify(ref _pool);
		target.Received -= Target_GivenTo;
		target.Released -= Target_TakenFrom;
	}
}
