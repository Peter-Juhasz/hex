using HexEditor.Composition;
using HexEditor.Core.ContentType;
using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavStructureTagger(
	[FromKeyedServices(WavContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<StructureTag>
{
	private static readonly StructureTag WavRiffTag = new("WAV RIFF Chunk");
	private static readonly StructureTag WavFormatTag = new("WAV Format Chunk");
	private static readonly StructureTag WavDataTag = new("WAV Data Chunk");
	private static readonly StructureTag WavFactTag = new("WAV Fact Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
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

		using var _ = ImmutableArrayBuilderPool<TagSpan<StructureTag>>.GetPooledObject(out var builder);
		foreach (var child in list.Children)
		{
			if (child is not TypeLengthChunkSyntaxNode chunkNode)
			{
				continue;
			}

			builder.Add(new TagSpan<StructureTag>(child.Span, chunkNode.TypeToken.Data.Span switch
			{
				{ } s when s.SequenceEqual("RIFF"u8) => WavRiffTag,
				{ } s when s.SequenceEqual("fmt "u8) => WavFormatTag,
				{ } s when s.SequenceEqual("fact"u8) => WavFactTag,
				{ } s when s.SequenceEqual("data"u8) => WavDataTag,
				_ => new StructureTag($"WAV {Encoding.ASCII.GetString(chunkNode.TypeToken.Data.Span)} Chunk")
			}));
		}
		return builder.ToImmutable();
	}
}
