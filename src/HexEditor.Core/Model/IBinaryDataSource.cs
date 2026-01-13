namespace HexEditor.Model;

public interface IBinaryDataSource : IAsyncDisposable
{
	ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}

public record class BinaryDataSourceSnapshot(IBinaryDataSource DataSource) : IBinarySnapshot
{
	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken) =>
		DataSource.CopyToAsync(offset, destination, cancellationToken);

	public long Length => DataSource.Length;

	public IBinarySnapshot Apply(BinaryChange change) => throw new NotImplementedException();
}
