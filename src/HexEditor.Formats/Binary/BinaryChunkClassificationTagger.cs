using HexEditor.Core.Classification;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Binary;

public abstract class BinaryChunkClassificationTagger(
	IPartialSyntaxTreeProvider syntaxTreeProvider
) 
	: AbstractSyntaxTreeTagger<ClassificationTag>(syntaxTreeProvider)
{
	protected override ImmutableArray<TagSpan<ClassificationTag>> GetTags(IPartialSyntaxTree syntaxTree, SnapshotSpan span, CancellationToken cancellationToken)
	{
		using var builder = new PooledArrayBuilder<TagSpan<ClassificationTag>>();
		foreach (var child in syntaxTree.Root.DescendantsAndSelf<TypeLengthChunkSyntaxNode>())
		{
			if (!span.OverlapsWith(child.Span))
			{
				continue;
			}

			if (span.End < child.Span.Start)
			{
				break;
			}

			if (child is not TypeLengthChunkSyntaxNode chunkNode)
			{
				continue;
			}

			if (!chunkNode.TypeToken.Span.IsEmpty)
			{
				builder.Add(new TagSpan<ClassificationTag>(chunkNode.TypeToken.Span, ClassificationTag.KeywordTag));
			}
		}
		return builder.ToImmutableArray();
	}
}
