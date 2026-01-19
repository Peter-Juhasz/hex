namespace HexEditor.Model;

public interface IBinaryDataSource : IAsyncDisposable
{
	ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
