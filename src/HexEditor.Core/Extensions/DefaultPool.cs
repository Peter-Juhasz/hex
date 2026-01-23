using Microsoft.Extensions.ObjectPool;

namespace HexEditor;

internal static class DefaultPool
{
	public const int MaximumItemCount = 512;

	public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy, int size = 20)
		where T : class
		=> new DefaultObjectPool<T>(policy, size);

	public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool)
		where T : class
		=> new(pool);

	public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool, out T obj)
		where T : class
	{
		var pooledObject = pool.GetPooledObject();
		obj = pooledObject.Object;
		return pooledObject;
	}
}
