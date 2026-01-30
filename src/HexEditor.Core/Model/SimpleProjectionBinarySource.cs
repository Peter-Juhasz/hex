using HexEditor.Model;

namespace HexEditor.Core.Model;

public class SimpleProjectionBinarySource(SnapshotSpan span) : IBinaryDataSource
{
	public long Length { get; } = span.Length;

	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken) =>
		span.Snapshot.CopyToAsync(offset, destination, cancellationToken);
}
