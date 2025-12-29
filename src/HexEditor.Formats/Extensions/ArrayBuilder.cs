using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HexEditor;

public ref struct PooledArrayBuilder<T> : IDisposable
{
	public PooledArrayBuilder()
	{
		Unsafe.SkipInit(out _element0);
		Unsafe.SkipInit(out _element1);
		Unsafe.SkipInit(out _element2);
	}

	private PooledObject<ImmutableArray<T>.Builder>? _pooledBuilder;

	private const int InlineCapacity = 3;
	private T _element0;
	private T _element1;
	private T _element2;

	private int _count = 0;

	public void Add(T item)
	{
		if (_count < InlineCapacity)
		{
			switch (_count++)
			{
				case 0: _element0 = item; break;
				case 1: _element1 = item; break;
				case 2: _element2 = item; break;
			}
			return;
		}

		if (_pooledBuilder is not { Object: { } builder })
		{
			_pooledBuilder = ImmutableArrayBuilderPool<T>.GetPooledObject(out builder);
			builder.AddRange(_element0, _element1, _element2);
		}

		builder.Add(item);
		_count++;
	}

	public void AddRange(ReadOnlySpan<T> values)
	{
		if (values.IsEmpty)
		{
			return;
		}

		if (_pooledBuilder is { Object: { } builder })
		{
			builder.AddRange(values);
			_count += values.Length;
			return;
		}

		foreach (var value in values)
		{
			Add(value);
		}
	}

	public readonly bool Any() => Count > 0;

	public readonly bool Any(Func<T, bool> predicate)
	{
		if (_pooledBuilder is { Object: { } builder })
		{
			return builder.Any(predicate);
		}

		foreach (var i in this)
		{
			if (predicate(i))
			{
				return true;
			}
		}

		return false;
	}

	public readonly bool Contains(T item, IEqualityComparer<T>? comparer = null)
	{
		if (_pooledBuilder is { Object: { } builder })
		{
			return builder.Contains(item, comparer);
		}

		comparer ??= EqualityComparer<T>.Default;
		foreach (var i in this)
		{
			if (comparer.Equals(i, item))
			{
				return true;
			}
		}

		return false;
	}

	public T this[int index]
	{
		readonly get
		{
			ArgumentOutOfRangeException.ThrowIfNegative(index);
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

			if (_pooledBuilder is { } builder)
			{
				return builder.Object[index];
			}

			return index switch
			{
				0 => _element0,
				1 => _element1,
				2 => _element2,
				_ => throw new ArgumentOutOfRangeException(nameof(index), "Index out of range."),
			};
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(index);
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

			if (_pooledBuilder is { Object: { } builder })
			{
				builder[index] = value;
				return;
			}

			switch (index)
			{
				case 0: _element0 = value; break;
				case 1: _element1 = value; break;
				case 2: _element2 = value; break;
				default:
					throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
			}
		}
	}

	public readonly int Count => _count;

	public void Clear()
	{
		if (_pooledBuilder is { } builder)
		{
			builder.Dispose();
			_pooledBuilder = null;
		}

		_count = 0;
		_element0 = default!;
		_element1 = default!;
		_element2 = default!;
	}

	public readonly T[] ToArray()
	{
		if (_pooledBuilder is { Object: { } builder })
		{
			return builder.ToArray();
		}

		return _count switch
		{
			1 => [_element0],
			2 => [_element0, _element1],
			3 => [_element0, _element1, _element2],
			0 => [],
			_ => throw new InvalidOperationException("Invalid count."),
		};
	}

	public readonly T[]? ToArrayOrNull()
	{
		if (!Any())
		{
			return null;
		}

		return ToArray();
	}

	public readonly ImmutableArray<T> ToImmutableArray()
	{
		if (_count == 0)
		{
			return [];
		}

		if (_pooledBuilder is { Object: { } builder })
		{
			return builder.ToImmutableArray();
		}

		var array = ToArray();
		return ImmutableCollectionsMarshal.AsImmutableArray(array);
	}

	public readonly List<T> ToMutableList()
	{
		var list = new List<T>(Count);

		if (Count <= InlineCapacity)
		{
			foreach (var item in this)
			{
				list.Add(item);
			}
		}
		else
		{
			list.AddRange(_pooledBuilder!.Value.Object);
		}

		return list;
	}

	public readonly Enumerator GetEnumerator() => new(this);

	public void Dispose()
	{
		Clear();
	}


	public ref struct Enumerator(PooledArrayBuilder<T> builder) : IEnumerator<T>
	{
		private readonly PooledArrayBuilder<T> _builder = builder;
		private int _index = -1;

		public readonly T Current => _builder[_index];

		readonly object? System.Collections.IEnumerator.Current => Current;

		public bool MoveNext()
		{
			_index++;
			return _index < _builder.Count;
		}

		public void Reset() => _index = -1;

		public readonly void Dispose() { }
	}
}
