using System;

namespace Open.Disposable
{
	public interface ITrimmableObjectPool
    {
		event ObjectPoolResizeEvent Received;
		event ObjectPoolResizeEvent Released;

		void TrimTo(int targetSize);
    }
}
