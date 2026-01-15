using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexViewRow
{
	IHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds VisualBounds { get; }

	ImmutableArray<FormattedSpan> HexRuns { get; }

	ImmutableArray<FormattedSpan> AsciiRuns { get; }
}

public class ViewRow(IHexView view, ViewportBounds bounds, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<FormattedSpan> hexRuns, ImmutableArray<FormattedSpan> asciiRuns) : IHexViewRow
{
	public IHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds VisualBounds { get; } = bounds;

	public ImmutableArray<FormattedSpan> HexRuns { get; } = hexRuns;

	public ImmutableArray<FormattedSpan> AsciiRuns { get; } = asciiRuns;
}
