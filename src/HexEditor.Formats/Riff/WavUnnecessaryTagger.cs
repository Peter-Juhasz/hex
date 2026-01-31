using HexEditor.Composition;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Core.Unnecessary;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavUnnecessaryTagger(
	[FromKeyedServices(WavContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : AbstractSyntaxTreeTagger<UnnecessaryTag>(syntaxTreeProvider)
{
	protected override ImmutableArray<TagSpan<UnnecessaryTag>> GetTags(IPartialSyntaxTree syntaxTree, SnapshotSpan span, CancellationToken cancellationToken)
	{
		if (syntaxTree.Root is not SyntaxNodeList { Children: [TypeLengthChunkSyntaxNode firstChunk, ..] })
		{
			return [];
		}

		if (!firstChunk.TypeToken.Data.Span.SequenceEqual("RIFF"u8))
		{
			return [];
		}

		if (8L + firstChunk.LengthToken.Value < span.Snapshot.Length)
		{
			return [];
		}

		return [new(span.Snapshot.Slice(8L + firstChunk.LengthToken.Value), UnnecessaryTag.Instance)];
	}
}
