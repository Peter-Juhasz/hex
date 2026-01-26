using HexEditor.Core.ReferenceHighlight;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Binary;

public abstract class BinaryChunkReferenceTagger(
	IViewAccessor viewAccessor,
	IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<ReferenceTag>
{
	public async Task<ImmutableArray<TagSpan<ReferenceTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var caret = viewAccessor.View.Caret.Position.Point;

		var syntaxTree = await syntaxTreeProvider.GetSyntaxTreeAsync(span, cancellationToken).ConfigureAwait(false);
		if (syntaxTree == null)
		{
			return [];
		}

		var node = syntaxTree.Root.DescendantsAndSelf<TypeLengthChunkSyntaxNode>().FirstOrDefault(n => n.TypeToken.Span.Contains(caret));
		if (node == null)
		{
			return [];
		}

		using var builder = new PooledArrayBuilder<TagSpan<ReferenceTag>>();
		foreach (var otherNode in syntaxTree.Root.DescendantsAndSelf<TypeLengthChunkSyntaxNode>())
		{
			if (!span.OverlapsWith(otherNode.TypeToken.Span))
			{
				continue;
			}

			if (span.End < otherNode.Span.Start)
			{
				break;
			}

			if (!otherNode.TypeToken.Data.Span.SequenceEqual(node.TypeToken.Data.Span))
			{
				continue;
			}

			builder.Add(new(otherNode.TypeToken.Span, ReferenceTag.ReferenceUsageTag));
		}
		return builder.ToImmutableArray();
	}
}
