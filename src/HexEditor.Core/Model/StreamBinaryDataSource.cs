namespace HexEditor.Core.Model;

public class StreamBinaryDataSource(Stream stream) : IBinaryDataSource
{
	public long Length { get; } = stream.Length;

	public async ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
		if (offset < 0 ) throw new ArgumentOutOfRangeException(nameof(offset));

		stream.Seek(offset, SeekOrigin.Begin);
		await stream.ReadExactlyAsync(destination, cancellationToken).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync() => stream.DisposeAsync();
}