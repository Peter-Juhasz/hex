namespace HexEditor.Model;

public struct OverlappingChunkMemoryReader(long totalLength, int chunkLength, int overlapLength)
{
	private readonly long _totalLength = totalLength;
	private readonly byte[] _buffer = new byte[chunkLength];
	private readonly int _overlapLength = overlapLength;

	private long _position = 0;
	private bool _started = false;

	public readonly long TotalLength => _totalLength;
	public readonly int ChunkLength => _buffer.Length;
	public readonly int OverlapLength => _overlapLength;
	public readonly long Position => _position;

	public readonly int CurrentLength => (int)Math.Max(0, Math.Min((long)_buffer.Length, _totalLength - _position));

	public readonly Memory<byte> Memory => _buffer.AsMemory(0, CurrentLength);

	public bool MoveNext(out Memory<byte> memory)
	{
		if (_totalLength <= 0)
		{
			memory = default;
			return false;
		}

		if (!_started)
		{
			_started = true;
			memory = Memory;
			return CurrentLength > 0;
		}

		// Advance by stride = chunk - overlap, ensuring progress.
		int stride = _buffer.Length - _overlapLength;
		if (stride <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(overlapLength), "overlapLength must be < chunkLength to ensure forward progress.");
		}

		long nextPos = _position + stride;
		if (nextPos >= _totalLength)
		{
			memory = default;
			return false;
		}

		_position = nextPos;
		memory = Memory;
		return CurrentLength > 0;
	}
}

public static partial class Extensions
{
	extension(SnapshotSpan span)
	{
		public OverlappingChunkMemoryReader CreateOverlappingChunkReader(int chunkLength, int overlapLength) => new(span.Span.Length, chunkLength, overlapLength);
	}
}