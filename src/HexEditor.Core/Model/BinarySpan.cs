namespace HexEditor.Model;

public readonly record struct BinarySpan(long StartOffset, long Length)
{
    public long EndOffset => StartOffset + Length;
}

public readonly record struct MemoryBinarySpan(long StartOffset, int Length)
{
    public long EndOffset => StartOffset + Length;

    public static implicit operator BinarySpan(MemoryBinarySpan span) => new(span.StartOffset, span.Length);
}
