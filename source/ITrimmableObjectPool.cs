namespace Open.Disposable
{
	public interface ITrimmableObjectPool
	{
		bool CountTrackingEnabled { get; }

		event ObjectPoolResizeEvent Received;
		event ObjectPoolResizeEvent Released;

		void TrimTo(int targetSize);
	}
}
