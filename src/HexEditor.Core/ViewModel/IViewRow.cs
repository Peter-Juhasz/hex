using HexEditor.Model;

namespace HexEditor.ViewModel;

public interface IViewRow
{
	IHexView View { get; }

	long RowIndex { get; }

	MemoryBinarySpan Span { get; }

	ReadOnlySpan<byte> Data { get; }
}

public class ViewRow(IHexView view, long rowIndex, MemoryBinarySpan span, ReadOnlyMemory<byte> dataView) : IViewRow
{
	public IHexView View { get; } = view;

	public long RowIndex { get; } = rowIndex;

	public MemoryBinarySpan Span { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;
}