namespace HexEditor.Core.Model;

public class StreamBinaryDataSource(Stream stream) : IBinaryDataSource
{
	public long Length { get; } = stream.Length;

	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
		ArgumentOutOfRangeException.ThrowIfNegative(offset);

		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			stream.Seek(offset, SeekOrigin.Begin);
			await stream.ReadExactlyAsync(destination, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async ValueTask DisposeAsync()
	{
		await stream.DisposeAsync();
		_semaphore.Dispose();
	}
}