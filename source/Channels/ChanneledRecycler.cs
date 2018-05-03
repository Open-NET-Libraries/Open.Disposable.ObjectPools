using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Open.Disposable
{
	/// <summary>
	/// This class is provided as an asynchronous queue for recycling instead of using a recycle delegate with an object pool and calling GiveAsync() which could pile up unnecessarily.
	/// So if recycling an object takes extra time, this might be a good way to toss objects away and not have to worry about the heavy cost as they will one by one be processed back into the target pool.
	/// </summary>
	public sealed class ChanneledRecycler<T> : RecyclerBase<T>
		where T : class
	{
		// Something else could be used and could be more performant.
		// But it's an ideal interface for what's needed.  And the point is that the recyler should not take up too much cpu time.
		Channel<T> _bin;

		internal ChanneledRecycler(
			IObjectPool<T> target,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY) : base(target, recycleFunction, limit)
		{
			var b = _bin = Channel.CreateBounded<T>(new BoundedChannelOptions(limit)
			{
				FullMode = BoundedChannelFullMode.DropWrite
			});

			async Task ProcessAsync()
			{
				var reader = _bin.Reader;
				while (Target != null
					&& await reader.WaitToReadAsync().ConfigureAwait(false))
				{
					while (Target != null
						&& reader.TryRead(out T item))
					{
						recycleFunction(item);
						Target?.Give(item);
					}
				}
			}

			Completion = ProcessAsync().ContinueWith(
					t => t.IsCompleted
						? b.Reader.Completion
						: t)
					.Unwrap();
		}

		internal ChanneledRecycler(
			ushort limit,
			IObjectPool<T> pool,
			Action<T> recycleFunction) : this(pool, recycleFunction, limit)
		{

		}

		public override bool Recycle(T item)
			=> _bin?.Writer.TryWrite(item) ?? false;

		protected override void OnCloseRequested()
			=> _bin?.Writer.Complete();

		protected override void OnDispose(bool calledExplicitly)
		{
			_bin?.Writer.Complete();
			_bin = null;
			Target = null;
		}
	}

	public static class ChanneledRecycler
	{
		public static ChanneledRecycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			Action<T> recycleFunction,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ChanneledRecycler<T>(pool, recycleFunction, limit);
		}

		public static ChanneledRecycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit,
			Action<T> recycleFunction)
			where T : class
		{
			return new ChanneledRecycler<T>(pool, recycleFunction, limit);
		}

		public static void Recycle(IRecyclable r)
		{
			r.Recycle();
		}

		public static ChanneledRecycler<T> CreateRecycler<T>(
			this IObjectPool<T> pool,
			ushort limit = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable
		{
			return new ChanneledRecycler<T>(pool, Recycle, limit);
		}

	}


}
