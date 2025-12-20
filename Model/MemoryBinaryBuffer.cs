namespace HexEditor.Model;

public class MemoryBinaryBuffer(ReadOnlyMemory<byte> buffer) : IBinaryBuffer
{
	public int Length => buffer.Length;

	long IBinaryBuffer.Length { get; }

	public ValueTask CopyToAsync(Memory<byte> destination, long offset, long length, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, destination.Length);

		if (!TryRead(destination.Span, offset, (int)length))
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}

		return ValueTask.CompletedTask;
	}

	public bool TryRead(Span<byte> destination, long offset, int length)
	{
		if (offset + length > int.MaxValue)
		{
			return false;
		}

		if (offset < 0 || length < 0 || offset + length > Length)
		{
			return false;
		}

		if (destination.Length < length)
		{
			return false;
		}

		var sourceSpan = buffer.Slice((int)offset, length);
		sourceSpan.Span.CopyTo(destination);
		return true;
	}
}
