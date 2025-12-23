namespace HexEditor.ViewModel;

public interface IViewRow
{
	IHexView View { get; }

	long RowIndex { get; }

	long Offset { get; }

	int Length { get; }

	ReadOnlySpan<byte> Data { get; }
}

public class ViewRow(IHexView view, long rowIndex, long offset, int length, ReadOnlyMemory<byte> dataView) : IViewRow
{
	public IHexView View { get; } = view;

	public long RowIndex { get; } = rowIndex;

	public long Offset { get; } = offset;

	public int Length { get; } = length;

	public ReadOnlySpan<byte> Data => dataView.Span;
}