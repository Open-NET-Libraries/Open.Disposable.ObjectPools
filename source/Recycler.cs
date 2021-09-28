using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Open.Disposable
{
	/// <inheritdoc />
	public class Recycler<T> : RecyclerBase<T>
		where T : class
	{
		Channel<T> _bin;

		internal Recycler(
			IObjectPool<T> target,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY) : base(target, recycleFunction)
		{
			_bin = Channel.CreateBounded<T>(limit);
			Completion = Processor(recycleFunction);
		}

		async Task Processor(Action<T> recycleFunction)
		{
			var bin = _bin;
			do
			{
				while (bin.Reader.TryRead(out var item))
				{
					recycleFunction(item);
					Target?.Give(item);
				}
			}
			while (await bin.Reader.WaitToReadAsync().ConfigureAwait(false));
		}

		internal Recycler(
			ushort limit,
			IObjectPool<T> pool,
			Action<T> recycleFunction) : this(pool, recycleFunction, limit) { }

		/// <inheritdoc />
		public override bool Recycle(T item)
			=> _bin?.Writer.TryWrite(item) ?? false;

		protected override void OnCloseRequested()
			=> _bin?.Writer.Complete();

		protected override void OnDispose()
		{
			base.OnDispose();

			_bin.Writer.TryComplete();
			_bin = null!;
		}
	}

	public static class Recycler
	{
		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class
			=> new(pool, recycleFunction, limit);

		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit,
			Action<T> recycleFunction)
			where T : class
			=> new(pool, recycleFunction, limit);

		public static void Recycle(IRecyclable r)
			=> (r ?? throw new ArgumentNullException(nameof(r))).Recycle();

		public static Recycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable
			=> new(pool, Recycle, limit);
	}
}
