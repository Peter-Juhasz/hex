namespace HexEditor.Model;

public interface IBinarySnapshot
{
	IBinaryDataSource Source { get; }

	ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken);

    long Length { get; }

	IBinarySnapshot? Previous { get; }
}

public static partial class Extensions
{
	extension(IBinarySnapshot snapshot)
	{
		public SnapshotPoint Start => new(snapshot, 0);

		public SnapshotSpan Span => new(snapshot, new(0, snapshot.Length));

		public SnapshotSpan Slice(long offset, long length)
		{
			if (offset < 0 || length < 0 || offset + length > snapshot.Length)
			{
				throw new ArgumentOutOfRangeException();
			}

			return new SnapshotSpan(snapshot, new LongSpan(offset, length));
		}

		public SnapshotSpan Slice(long offset) => snapshot.Slice(offset, snapshot.Length - offset);
	}
}