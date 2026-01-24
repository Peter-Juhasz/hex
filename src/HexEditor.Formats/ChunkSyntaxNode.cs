using HexEditor.Core.Syntax;
using HexEditor.Model;

namespace HexEditor.Formats;

public record class TypeLengthChunkSyntaxNode(
	SnapshotSpan Span,
	SyntaxToken TypeToken,
	Int32SyntaxToken LengthToken
) : SyntaxNode(
	Span
);
