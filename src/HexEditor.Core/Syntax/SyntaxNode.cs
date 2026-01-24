using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public abstract record class SyntaxNode(
	SnapshotSpan Span
);

public readonly record struct SyntaxToken(
	SnapshotSpan Span,
	ReadOnlyMemory<byte> Data
);

public readonly record struct Int32SyntaxToken(
	SnapshotSpan Span,
	int Value
);