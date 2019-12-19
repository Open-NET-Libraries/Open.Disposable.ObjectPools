using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Open.Disposable
{
	/// <summary>
	/// This class is provided as an asynchronous queue for recycling instead of using a recycle delegate with an object pool and calling GiveAsync() which could pile up unnecessarily.
	/// So if recycling an object takes extra time, this might be a good way to toss objects away and not have to worry about the heavy cost as they will one by one be processed back into the target pool.
	/// </summary>
	// ReSharper disable once InheritdocConsiderUsage
	public abstract class RecyclerBase<T> : DisposableBase, IRecycler<T>
		where T : class
	{
		protected IObjectPool<T> Target;

		protected RecyclerBase(
			IObjectPool<T> target,
			Action<T> recycleFunction)
		{
			if (recycleFunction == null) throw new ArgumentNullException(nameof(recycleFunction));
			Target = target ?? throw new ArgumentNullException(nameof(target));
			Contract.EndContractBlock();

			if (!(target is DisposableBase d)) return;
			if (d.WasDisposed) throw new ArgumentException("Cannot recycle for an object pool that is already disposed.");
			d.BeforeDispose += Pool_BeforeDispose;
			// Could possibly dispose before this line somewhere... But that's just nasty. :P 
		}

		void Pool_BeforeDispose(object sender, EventArgs e) => Dispose();

		public abstract bool Recycle(T item);

		protected abstract void OnCloseRequested();

		// ReSharper disable once MemberCanBeProtected.Global
		public Task Completion { get; protected set; } = Task.CompletedTask;

		public Task Close()
		{
			OnCloseRequested();
			return Completion;
		}

		protected override void OnDispose()
		{
			OnCloseRequested();
			Target = null!;
		}
	}

}
