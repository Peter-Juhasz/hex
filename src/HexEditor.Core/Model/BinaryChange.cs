namespace HexEditor.Model;

public readonly record struct BinaryChange(LongSpan Span, ReadOnlyMemory<byte> NewData);

public static partial class Extensions
{
	extension(BinaryChange)
	{
		public static BinaryChange Insert(long offset, ReadOnlyMemory<byte> data) =>
			new(new(offset, 0), data);

		public static BinaryChange Delete(LongSpan span) =>
			new(span, ReadOnlyMemory<byte>.Empty);

		public static BinaryChange Replace(LongSpan span, ReadOnlyMemory<byte> newData) =>
			new(span, newData);
	}

	extension(BinaryChange change)
	{
		public long OldLength => change.Span.Length;

		public long NewLength => change.NewData.Length;

		public long Delta => change.NewLength - change.OldLength;

		public LongSpan OldSpan => change.Span;

		public LongSpan NewSpan => new(change.Span.StartOffset, change.NewData.Length);
	}
}