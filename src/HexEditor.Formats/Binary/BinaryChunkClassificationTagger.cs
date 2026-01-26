using HexEditor.Core.Classification;
using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Binary;

public abstract class BinaryChunkClassificationTagger(
	IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<ClassificationTag>
{
	public async Task<ImmutableArray<TagSpan<ClassificationTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var syntaxTree = await syntaxTreeProvider.GetSyntaxTreeAsync(span, cancellationToken).ConfigureAwait(false);
		if (syntaxTree == null)
		{
			return [];
		}

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
