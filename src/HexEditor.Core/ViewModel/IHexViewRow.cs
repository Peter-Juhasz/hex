using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexViewRow
{
	IHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds VisualBounds { get; }

	ImmutableArray<FormattedTextRun> HexRuns { get; }

	ImmutableArray<FormattedTextRun> AsciiRuns { get; }
}

public class ViewRow(IHexView view, ViewportBounds bounds, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<FormattedTextRun> hexRuns, ImmutableArray<FormattedTextRun> asciiRuns) : IHexViewRow
{
	public IHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public ViewportBounds VisualBounds { get; } = bounds;

	public ImmutableArray<FormattedTextRun> HexRuns { get; } = hexRuns;

	public ImmutableArray<FormattedTextRun> AsciiRuns { get; } = asciiRuns;
}
