using HexEditor.Core.Model;

namespace HexEditor.Model;

public readonly struct SnapshotSpan : IEquatable<SnapshotSpan>
{
	public SnapshotSpan(IBinarySnapshot snapshot, LongSpan span)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(span.EndOffset, snapshot.Length);

		Snapshot = snapshot;
		Span = span;
	}

	public static SnapshotSpan Create(SnapshotPoint point, long length) =>
		new(point.Snapshot, new LongSpan(point.Position, length));

	public IBinarySnapshot Snapshot { get; }
	public LongSpan Span { get; }


	public SnapshotPoint this[long offset] => new(Snapshot, Span.StartOffset + offset);


	public bool Equals(SnapshotSpan other) => this == other;

	public override bool Equals(object? obj) => obj is SnapshotSpan other && this == other;

	public override int GetHashCode() => HashCode.Combine(Snapshot, Span);


	public static bool operator ==(SnapshotSpan left, SnapshotSpan right) =>
		left.Snapshot == right.Snapshot && left.Span == right.Span;

	public static bool operator !=(SnapshotSpan left, SnapshotSpan right) =>
		!(left == right);
}

public static partial class Extensions
{
	extension(SnapshotSpan)
	{
		public static SnapshotSpan Create(SnapshotPoint start, SnapshotPoint end)
		{
			SnapshotMismatchException.ThrowIfMismatch(start.Snapshot, end.Snapshot);

			if (start.Position > end.Position)
			{
				throw new ArgumentOutOfRangeException(nameof(end), "End position must be greater than or equal to start position.");
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

		// TODO: throw or not?

		public bool Contains(SnapshotPoint point)
		{
			SnapshotMismatchException.ThrowIfMismatch(span.Snapshot, point.Snapshot);
			return span.Span.Contains(point.Position);
		}

		public bool Contains(SnapshotSpan other)
		{
			SnapshotMismatchException.ThrowIfMismatch(span.Snapshot, other.Snapshot);
			return span.Span.Contains(other.Span);
		}

		public bool OverlapsWith(SnapshotSpan other)
		{
			SnapshotMismatchException.ThrowIfMismatch(span.Snapshot, other.Snapshot);
			return span.Span.OverlapsWith(other.Span);
		}

		public long Length => span.Span.Length;

		public bool IsEmpty => span.Span.IsEmpty;

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
