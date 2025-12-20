namespace HexEditor.Model;

public class StreamBinaryBuffer(Stream stream) : IBinaryBuffer
{
	public long Length => stream.Length;

	public async ValueTask CopyToAsync(Memory<byte> destination, long offset, long length, CancellationToken cancellationToken)
	{
		if (offset < 0 || offset + length > stream.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}

		var buffer = destination[..(int)length];
		stream.Seek(offset, SeekOrigin.Begin);
		await stream.ReadExactlyAsync(buffer, cancellationToken);
	}

	public bool TryRead(Span<byte> buffer, long offset, int length)
	{
		if (offset < 0 || offset + length > stream.Length)
		{
			return false;
		}

		if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var segment))
		{
			segment.AsSpan((int)offset, length).CopyTo(buffer);
			return true;
		}

		return false;
	}

	public ValueTask DisposeAsync() => stream.DisposeAsync();
}