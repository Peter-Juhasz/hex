namespace HexEditor.Model;

public readonly record struct SnapshotSpan(IBinarySnapshot Snapshot, LongSpan Span);

public static partial class Extensions
{
	extension(SnapshotSpan)
	{
		public static SnapshotSpan Create(SnapshotPoint start, SnapshotPoint end)
		{
			if (start.Snapshot != end.Snapshot)
			{
				throw new ArgumentException("SnapshotPoints must belong to the same snapshot.");
			}

			if (start.Position > end.Position)
			{
				throw new ArgumentOutOfRangeException(nameof(end), "End position must be greater than or equal to start position.");
			}

			if (start.Position < 0 || end.Position < 0 || start.Position > start.Snapshot.Length || end.Position > end.Snapshot.Length)
			{
				throw new ArgumentOutOfRangeException("SnapshotPoint positions must be within the bounds of the snapshot.");
			}

			var span = new LongSpan(start.Position, end.Position - start.Position);
			return new SnapshotSpan(start.Snapshot, span);
		}
	}

	extension(SnapshotSpan span)
	{
		public SnapshotPoint Start => new(span.Snapshot, span.Span.StartOffset);

		public SnapshotPoint End => new(span.Snapshot, span.Span.EndOffset);

		public SnapshotSpan Slice(long offset) =>
			new(span.Snapshot, span.Span.Slice(offset));

		public SnapshotSpan Slice(long offset, long length) =>
			new(span.Snapshot, span.Span.Slice(offset, length));

		public bool Contains(SnapshotPoint point) =>
			span.Snapshot == point.Snapshot && span.Span.Contains(point.Position);

		// TODO bad API desing
		public ValueTask CopyToAsync(Memory<byte> destination, CancellationToken cancellationToken)
		{
			if (span.Span.Length > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(span), "Span length exceeds maximum supported size.");
			}

			if (destination.Length < span.Span.Length)
			{
				throw new ArgumentException("Destination buffer is smaller than the span length.", nameof(destination));
			}

			var spanLength = (int)span.Span.Length;
			if (destination.Length > spanLength)
			{
				destination = destination[..spanLength];
			}

            return span.Snapshot.CopyToAsync(span.Span.StartOffset, destination, cancellationToken);
		}
	}
}
