using HexEditor.Model;

namespace HexEditor.ViewModel;

public readonly record struct FormattedTextRun(
	SnapshotSpan Span,
	ReadOnlyMemory<byte> Data,
	string Text,
	double LeftPosition,
	double RenderedWidth,
	object? Style
);
