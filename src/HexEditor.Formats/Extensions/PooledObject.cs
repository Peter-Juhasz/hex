using Microsoft.Extensions.ObjectPool;

namespace HexEditor;

public struct PooledObject<T> : IDisposable
	where T : class
{
	private readonly ObjectPool<T> _pool;
	private T? _object;

	public readonly T Object => _object!;

	public PooledObject(ObjectPool<T> pool)
		: this()
	{
		_pool = pool;
		_object = pool.Get();
	}

	public void Dispose()
	{
		if (_object is { } obj)
		{
			_pool.Return(obj);
			_object = null;
		}
	}
}
