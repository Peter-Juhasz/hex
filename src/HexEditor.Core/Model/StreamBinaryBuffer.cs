namespace HexEditor.Model;

public class StreamBinaryBuffer(Stream stream) : IBinaryBuffer
{
	public long Length => stream.Length;

	public async ValueTask CopyToAsync(MemoryBinarySpan span, Memory<byte> destination, CancellationToken cancellationToken)
    {
		var buffer = destination[..span.Length];
		stream.Seek(span.StartOffset, SeekOrigin.Begin);
		await stream.ReadExactlyAsync(buffer, cancellationToken);
	}

	public ValueTask DisposeAsync() => stream.DisposeAsync();
}