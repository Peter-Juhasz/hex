namespace HexEditor.Model;

public readonly struct LongSpan : IEquatable<LongSpan>
{
	public LongSpan(long startOffset, long length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		StartOffset = startOffset;
		Length = length;
	}

	public long StartOffset { get; }
	public long Length { get; }


	public bool Equals(LongSpan other) => this == other;

	public override bool Equals(object? obj) => obj is LongSpan other && this == other;

	public override int GetHashCode() => HashCode.Combine(StartOffset, Length);


	public static bool operator ==(LongSpan left, LongSpan right) =>
		left.StartOffset == right.StartOffset && left.Length == right.Length;

	public static bool operator !=(LongSpan left, LongSpan right) =>
		!(left == right);
}

public static partial class Extensions
{
	extension(LongSpan span)
	{
		public long EndOffset => span.StartOffset + span.Length;

		public bool OverlapsWith(LongSpan other)
		{
			long overlapStart = Math.Max(span.StartOffset, other.StartOffset);
			long overlapEnd = Math.Min(span.EndOffset, other.EndOffset);

			return overlapStart < overlapEnd;
		}

		public bool Contains(long offset) => offset >= span.StartOffset && offset < span.EndOffset;

		public bool Contains(LongSpan other) => other.StartOffset >= span.StartOffset && other.EndOffset <= span.EndOffset;

		public bool IntersectsWith(LongSpan other) => other.StartOffset <= span.EndOffset && other.EndOffset >= span.StartOffset;

		public bool IsEmpty => span.Length == 0;

		public LongSpan Slice(long offset, long length) => new(span.StartOffset + offset, length);

		public LongSpan Slice(long offset) => span.Slice(offset, span.Length - offset);
	}
}