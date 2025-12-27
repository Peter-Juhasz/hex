namespace HexEditor.Model;

public class MemoryBinaryBuffer(ReadOnlyMemory<byte> buffer) : IBinaryBuffer
{
	public int Length => buffer.Length;

	long IBinaryBuffer.Length { get; }

	public ValueTask CopyToAsync(MemoryBinarySpan span, Memory<byte> destination, CancellationToken cancellationToken)
    {
		if (!TryRead(destination.Span, span))
		{
			throw new ArgumentOutOfRangeException(nameof(span));
		}

		return ValueTask.CompletedTask;
	}

	public bool TryRead(Span<byte> destination, MemoryBinarySpan span)
	{
		if (span.EndOffset > int.MaxValue)
		{
			return false;
		}

		if (span.EndOffset >= Length)
		{
			return false;
		}

		if (destination.Length < span.Length)
		{
			return false;
		}

		var sourceSpan = buffer.Slice((int)span.StartOffset, span.Length);
		sourceSpan.Span.CopyTo(destination);
		return true;
	}
}
