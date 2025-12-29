using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexViewRow
{
	IHexView View { get; }

	MemorySpan Span { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds Bounds { get; }

	ImmutableArray<FormattedSpan> FormattedRuns { get; }
}

public class ViewRow(IHexView view, ViewportBounds bounds, MemorySpan span, ReadOnlyMemory<byte> dataView) : IHexViewRow
{
	public IHexView View { get; } = view;

	public MemorySpan Span { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds Bounds { get; } = bounds;

	public ImmutableArray<FormattedSpan> FormattedRuns => throw new NotImplementedException();
}
