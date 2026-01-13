using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexViewRow
{
	IHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds VisualBounds { get; }

	ImmutableArray<FormattedSpan> FormattedRuns { get; }
}

public class ViewRow(IHexView view, ViewportBounds bounds, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<FormattedSpan> formattedRuns) : IHexViewRow
{
	public IHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds VisualBounds { get; } = bounds;

	public ImmutableArray<FormattedSpan> FormattedRuns { get; } = formattedRuns;
}
