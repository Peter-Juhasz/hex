namespace HexEditor.Core.Syntax;

public interface IPartialSyntaxTree
{
	SyntaxNode Root { get; }
}

public record class PartialSyntaxTree(
	SyntaxNode Root
) : IPartialSyntaxTree;