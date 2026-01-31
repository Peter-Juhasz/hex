using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Binary;

public abstract class BinaryChunkStructureTagger(
	IPartialSyntaxTreeProvider syntaxTreeProvider
) : AbstractSyntaxTreeTagger<StructureTag>(syntaxTreeProvider)
{
	protected override ImmutableArray<TagSpan<StructureTag>> GetTags(IPartialSyntaxTree syntaxTree, SnapshotSpan span, CancellationToken cancellationToken)
	{
		using var builder = new PooledArrayBuilder<TagSpan<StructureTag>>();
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

			builder.Add(new TagSpan<StructureTag>(child.Span, GetTag(chunkNode.TypeToken.Data.Span)));
		}
		return builder.ToImmutableArray();
	}

	protected abstract StructureTag GetTag(ReadOnlySpan<byte> type);
}
