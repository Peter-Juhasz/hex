using HexEditor.Composition;
using HexEditor.Core.Classification;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiClassifier(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<ClassificationTag>
{
	public async Task<ImmutableArray<TagSpan<ClassificationTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length < 8)
		{
			return [];
		}

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
			if (child is not TypeLengthChunkSyntaxNode chunkNode)
			{
				continue;
			}

			var isKnownChunk = chunkNode.TypeToken.Data.Span switch
			{
				{ } s when s.SequenceEqual("MThd"u8) => true,
				{ } s when s.SequenceEqual("MTrk"u8) => true,
				_ => false
			};
			if (isKnownChunk)
			{

			}
			builder.Add(new(chunkNode.TypeToken.Span, ClassificationTag.KeywordTag));
		}
		return builder.ToImmutable();
	}
}
