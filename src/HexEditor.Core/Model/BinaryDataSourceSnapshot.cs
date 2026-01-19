namespace HexEditor.Model;

public record class BinaryDataSourceSnapshot(IBinaryDataSource DataSource) : IBinarySnapshot
{
	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken) =>
		DataSource.CopyToAsync(offset, destination, cancellationToken);

	public long Length => DataSource.Length;

    public IBinaryDataSource Source => DataSource;

    public IBinarySnapshot? Previous => throw new NotImplementedException();
}
