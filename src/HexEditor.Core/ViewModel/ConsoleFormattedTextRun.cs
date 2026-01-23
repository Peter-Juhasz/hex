using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public readonly record struct ConsoleFormattedTextRun(
	SnapshotSpan Span,
	ReadOnlyMemory<byte> Data,
	string Text,
	int Offset,
	ImmutableArray<TagSpan> Tags,
	object? Style
)
{
	public int Length => (int)Span.Length;
}
