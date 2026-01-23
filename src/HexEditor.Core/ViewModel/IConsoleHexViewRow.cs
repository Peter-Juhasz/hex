using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public interface IConsoleHexViewRow
{
	IConsoleHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	long Index { get; }

	ImmutableArray<ConsoleFormattedTextRun> HexRuns { get; }

	ImmutableArray<ConsoleFormattedTextRun> AsciiRuns { get; }
}
