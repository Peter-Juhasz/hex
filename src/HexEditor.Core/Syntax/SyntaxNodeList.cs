using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Syntax;

public record class SyntaxNodeList(
	SnapshotSpan Span,
	IImmutableList<SyntaxNode> Children
) : SyntaxNode(Span)
{
	public SyntaxNodeList(IImmutableList<SyntaxNode> children) : this(
		Span: SnapshotSpan.Create(children[0].Span.Start, children[^1].Span.End),
		children
	)
	{ }

	public override IEnumerable<SyntaxNode> EnumerateChildren() => Children;
}