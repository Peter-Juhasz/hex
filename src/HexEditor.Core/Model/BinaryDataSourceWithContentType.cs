using HexEditor.Core.ContentType;

namespace HexEditor.Core.Model;

public sealed class BinaryDataSourceWithContentType(
	IBinaryDataSource inner,
	ContentTypeDefinition contentType
) : IBinaryDataSource
{
	public ContentTypeDefinition? ContentType => contentType;

	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken) =>
		inner.CopyToAsync(offset, destination, cancellationToken);

	public long Length => inner.Length;

	public ValueTask DisposeAsync() => inner.DisposeAsync();
}