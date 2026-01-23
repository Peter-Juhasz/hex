using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public class ConsoleHexViewRow(IConsoleHexView view, long index, SnapshotSpan span, ReadOnlyMemory<byte> dataView, ImmutableArray<ConsoleFormattedTextRun> hexRuns, ImmutableArray<ConsoleFormattedTextRun> asciiRuns) : IConsoleHexViewRow
{
	public IConsoleHexView View { get; } = view;

	public SnapshotSpan Extent { get; } = span;

	public ReadOnlySpan<byte> Data => dataView.Span;

	public long Index { get; } = index;

	public ImmutableArray<ConsoleFormattedTextRun> HexRuns { get; } = hexRuns;

	public ImmutableArray<ConsoleFormattedTextRun> AsciiRuns { get; } = asciiRuns;
}
