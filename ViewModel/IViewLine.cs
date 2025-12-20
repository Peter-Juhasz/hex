namespace HexEditor.ViewModel;

public interface IViewLine
{
	IHexView View { get; }

	long LineIndex { get; }

	long Offset { get; }

	int Length { get; }

	ReadOnlySpan<byte> Data { get; }
}

public class ViewLine(IHexView view, long lineIndex, long offset, int length, ReadOnlyMemory<byte> dataView) : IViewLine
{
	public IHexView View { get; } = view;

	public long LineIndex { get; } = lineIndex;

	public long Offset { get; } = offset;

	public int Length { get; } = length;

	public ReadOnlySpan<byte> Data => dataView.Span;
}