namespace HexEditor.Model;

public readonly record struct LongSpan(long StartOffset, long Length)
{
    public long EndOffset => StartOffset + Length;
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

		public LongSpan Slice(long offset, long length)
		{
			if (offset < 0 || length < 0 || offset + length > span.Length)
			{
				throw new ArgumentOutOfRangeException("Slice parameters are out of bounds of the LongSpan.");
			}

			return new LongSpan(span.StartOffset + offset, length);
		}

		public LongSpan Slice(long offset) => span.Slice(offset, span.Length - offset);
	}
}