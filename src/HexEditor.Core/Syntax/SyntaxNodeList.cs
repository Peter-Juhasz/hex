using HexEditor.Model;
using System.Collections;
using System.Collections.Immutable;

namespace HexEditor.Core.Syntax;

public record class SyntaxNodeList(
	SnapshotSpan Span,
	IImmutableList<SyntaxNode> Children
) 
	: SyntaxNode(Span), IReadOnlyList<SyntaxNode>
{
	public SyntaxNodeList(IImmutableList<SyntaxNode> children) : this(
		Span: SnapshotSpan.Create(children[0].Span.Start, children[^1].Span.End),
		children
	)
	{ }

	public SyntaxNode this[int index] => ((IReadOnlyList<SyntaxNode>)Children)[index];

	public int Count => Children.Count;

	public override IEnumerable<SyntaxNode> EnumerateChildren() => Children;

	public IEnumerator<SyntaxNode> GetEnumerator() => Children.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Children).GetEnumerator();
}