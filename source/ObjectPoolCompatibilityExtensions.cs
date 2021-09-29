namespace Open.Disposable.ObjectPoolCompatibility;

/// <summary>
/// Provided as means for compatiibility with object pools
/// that have the same interface as the Microsoft Extensions version.
/// </summary>
public static class ObjectPoolCompatibilityExtensions
{
	/// <inheritdoc cref="IObjectPool{T}.Take"/>
	public static T Get<T>(this IObjectPool<T> pool)
		where T : class => pool.Take();

	/// <inheritdoc cref="IObjectPool{T}.Give(T)"/>
	public static void Return<T>(this IObjectPool<T> pool, T item)
		where T : class => pool.Give(item);
}
