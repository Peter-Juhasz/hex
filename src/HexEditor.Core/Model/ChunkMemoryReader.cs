using System.Buffers;

namespace HexEditor.Model;

public struct ChunkMemoryReader : IDisposable
{
	public ChunkMemoryReader(long totalLength, int chunkLength)
	{
		_totalLength = totalLength;
		_buffer = Pool.Rent(totalLength < int.MaxValue ? Math.Min((int)totalLength, chunkLength) : chunkLength);
	}

	private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

	private long _position = -1L;
	private readonly long _totalLength;

	public readonly long TotalLength => _totalLength;

	private readonly byte[] _buffer;

	public readonly int ChunkLength => _buffer.Length;

	public readonly long Position => _position;

	public readonly Memory<byte> Memory => _buffer.AsMemory(0, (int)Math.Min(ChunkLength, _totalLength - _position));

	public bool MoveNext(out Memory<byte> memory)
	{
		if (_position == -1L)
		{
			_position = 0L;
		}
		else
		{
			_position += ChunkLength;
		}

		if (_position < _totalLength)
		{
			memory = Memory;
			return true;
		}

		memory = default;
		return false;
	}

	public void Dispose()
	{
		Pool.Return(_buffer);
	}
}

public static partial class Extensions
{
	extension(SnapshotSpan span)
	{
		public ChunkMemoryReader CreateChunkReader(int chunkLength) => new(span.Span.Length, chunkLength);
	}
}