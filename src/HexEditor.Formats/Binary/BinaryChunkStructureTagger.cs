using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Binary;

public abstract class BinaryChunkStructureTagger(
	IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<StructureTag>
{
	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
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

		using var _ = ImmutableArrayBuilderPool<TagSpan<StructureTag>>.GetPooledObject(out var builder);
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

			builder.Add(new TagSpan<StructureTag>(child.Span, GetTag(chunkNode.TypeToken.Data.Span)));
		}
		return builder.ToImmutable();
	}

	protected abstract StructureTag GetTag(ReadOnlySpan<byte> type);
}
