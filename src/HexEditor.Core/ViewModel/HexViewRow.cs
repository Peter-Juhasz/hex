using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public class HexViewRow(IHexView view, ViewportBounds bounds, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<FormattedTextRun> hexRuns, ImmutableArray<FormattedTextRun> asciiRuns) : IHexViewRow
{
	public IHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds VisualBounds { get; } = bounds;

	public ImmutableArray<FormattedTextRun> HexRuns { get; } = hexRuns;

	public ImmutableArray<FormattedTextRun> AsciiRuns { get; } = asciiRuns;
}
