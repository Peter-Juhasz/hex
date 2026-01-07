namespace HexEditor.Model;

public class MemoryBinaryBuffer(ReadOnlyMemory<byte> buffer) : IBinaryBuffer
{
	public long Length { get; } = buffer.Length;

	public ValueTask CopyToAsync(MemorySpan span, Memory<byte> destination, CancellationToken cancellationToken)
    {
		if (!TryRead(span, destination.Span))
		{
			throw new ArgumentOutOfRangeException(nameof(span));
		}

		return ValueTask.CompletedTask;
	}

	public bool TryRead(MemorySpan span, Span<byte> destination)
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
