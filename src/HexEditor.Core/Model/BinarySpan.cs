namespace HexEditor.Model;

public readonly record struct BinarySpan(long StartOffset, long Length)
{
    public long EndOffset => StartOffset + Length;
}