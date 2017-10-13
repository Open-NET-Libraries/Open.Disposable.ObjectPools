using System;

namespace Open.Disposable
{
	public interface ITrimmableObjectPool
    {
		event ObjectPoolResizeEvent GivenTo;
		event ObjectPoolResizeEvent TakenFrom;

		void TrimTo(int targetSize);
    }
}
