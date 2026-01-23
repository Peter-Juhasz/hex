namespace HexEditor.Model;

public struct ChunkMemoryReader
{
	public ChunkMemoryReader(long totalLength, int chunkLength)
		: this(totalLength, new byte[chunkLength])
	{ }
	public ChunkMemoryReader(long totalLength, byte[] buffer)
	{
		this.totalLength = totalLength;
		_buffer = buffer;
		_totalLength = totalLength;
	}

	private long _position = -1L;
	private readonly long _totalLength;

	public readonly long TotalLength => _totalLength;

	private readonly byte[] _buffer;
	private readonly long totalLength;

	public readonly int ChunkLength => _buffer.Length;

	public readonly long Position => _position;

	public readonly Memory<byte> Memory => _buffer.AsMemory(0, (int)Math.Min(ChunkLength, totalLength - _position));

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

		if (_position < totalLength)
		{
			memory = Memory;
			return true;
		}

		memory = default;
		return false;
	}
}

public static partial class Extensions
{
	extension(SnapshotSpan span)
	{
		public ChunkMemoryReader CreateChunkReader(int chunkLength) => new(span.Span.Length, chunkLength);
	}
}