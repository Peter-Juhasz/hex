namespace HexEditor.Model;

public readonly record struct LongSpan(long StartOffset, long Length)
{
    public long EndOffset => StartOffset + Length;

	public static explicit operator MemorySpan(LongSpan span) => new(span.StartOffset, (int)span.Length);
}

public readonly record struct MemorySpan(long StartOffset, int Length)
{
    public long EndOffset => StartOffset + Length;

    public static implicit operator LongSpan(MemorySpan span) => new(span.StartOffset, span.Length);
}


public static partial class Extensions
{
    extension(LongSpan span)
    {
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
	}

	extension(MemorySpan span)
	{
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
	}
}