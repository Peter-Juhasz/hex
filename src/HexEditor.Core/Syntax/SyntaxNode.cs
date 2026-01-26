using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public abstract record class SyntaxNode(
	SnapshotSpan Span
)
{
	public virtual IEnumerable<SyntaxNode> EnumerateChildren() => [];
}

public readonly record struct SyntaxToken(
	SnapshotSpan Span,
	ReadOnlyMemory<byte> Data
);

public readonly record struct Int32SyntaxToken(
	SnapshotSpan Span,
	int Value
);

public static partial class Extensions
{
	extension(SyntaxNode node)
	{
		public IEnumerable<TNode> DescendantsAndSelf<TNode>() where TNode : SyntaxNode =>
			node.DescendantsAndSelf().OfType<TNode>();

		public IEnumerable<SyntaxNode> DescendantsAndSelf()
		{
			yield return node;

			foreach (var child in node.EnumerateChildren())
			{
				foreach (var descendant in child.DescendantsAndSelf())
				{
					yield return descendant;
				}
			}
		}
	}
}