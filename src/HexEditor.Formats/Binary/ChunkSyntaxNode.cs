using HexEditor.Core.Syntax;
using HexEditor.Model;

namespace HexEditor.Formats.Binary;

public record class TypeLengthChunkSyntaxNode(
	SnapshotSpan Span,
	SyntaxToken TypeToken,
	Int32SyntaxToken LengthToken
) : SyntaxNode(
	Span
);
