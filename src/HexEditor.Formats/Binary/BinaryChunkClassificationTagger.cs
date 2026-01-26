using HexEditor.Core.Classification;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Formats.Binary;
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

		if (syntaxTree.Root is not SyntaxNodeList list)
		{
			return [];
		}

		using var _ = ImmutableArrayBuilderPool<TagSpan<ClassificationTag>>.GetPooledObject(out var builder);
		foreach (var child in list.Children)
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
		return builder.ToImmutable();
	}
}
