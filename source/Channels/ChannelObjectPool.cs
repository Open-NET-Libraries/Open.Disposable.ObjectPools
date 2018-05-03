using System;
using System.Threading.Channels;

namespace Open.Disposable
{
	public sealed class ChannelObjectPool<T> : ObjectPoolBase<T>
		where T : class
	{
		public ChannelObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
			: base(factory, recycler, capacity)
		{
			Pool = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.DropWrite });
		}

		public ChannelObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
			: this(factory, null, capacity)
		{ }

		Channel<T> Pool;

		protected override void OnDispose(bool calledExplicitly)
		{
			Pool?.Writer.Complete();
			Pool = null;
		}

		protected override bool Receive(T item)
		   => Pool?.Writer.TryWrite(item) ?? false;

		protected override T TryRelease()
		{
			T item = null;
			Pool?.Reader.TryRead(out item);
			return item;
		}
	}


	public static class ChannelObjectPool
	{
		public static ChannelObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class
		{
			return new ChannelObjectPool<T>(factory, capacity);
		}


		public static ChannelObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, new()
		{
			return Create(() => new T(), capacity);
		}

		public static ChannelObjectPool<T> Create<T>(Func<T> factory, bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable
		{
			Action<T> recycler = null;
			if (autoRecycle) recycler = Recycler.Recycle;
			return new ChannelObjectPool<T>(factory, recycler, capacity);
		}

		public static ChannelObjectPool<T> Create<T>(bool autoRecycle, int capacity = Constants.DEFAULT_CAPACITY)
			where T : class, IRecyclable, new()
		{
			return Create(() => new T(), autoRecycle, capacity);
		}
	}
}
