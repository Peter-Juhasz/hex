using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public interface IPartialSyntaxTree
{
	SyntaxNode Root { get; }

	SnapshotSpan CoveredSpan => Root.Span;
}

public record class PartialSyntaxTree(
	SyntaxNode Root
) : IPartialSyntaxTree;