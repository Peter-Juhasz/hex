namespace HexEditor.Model;

public readonly record struct SnapshotSpan(IBinarySnapshot Snapshot, LongSpan Span);

public static partial class Extensions
{
	extension(SnapshotSpan span)
	{
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

			destination = destination[..(int)span.Span.Length];

			return span.Snapshot.CopyToAsync(span.Span.StartOffset, destination, cancellationToken);
		}
	}
}