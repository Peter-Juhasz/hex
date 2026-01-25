using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public class HexViewRow(IGraphicalHexView view, ViewportBounds bounds, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<FormattedTextRun> hexRuns, ImmutableArray<FormattedTextRun> asciiRuns) : IHexViewRow
{
	public IGraphicalHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds VisualBounds { get; } = bounds;

	public double Baseline { get; } = bounds.Bottom - 2d;

	public ImmutableArray<FormattedTextRun> HexRuns { get; } = hexRuns;

	public ImmutableArray<FormattedTextRun> AsciiRuns { get; } = asciiRuns;
}
