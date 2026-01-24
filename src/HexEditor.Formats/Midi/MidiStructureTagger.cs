using HexEditor.Composition;
using HexEditor.Core.ContentType;
using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiStructureTagger(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<StructureTag>
{
	private static readonly StructureTag MidiHeaderTag = new("MIDI Header Chunk");
	private static readonly StructureTag MidiTrackTag = new("MIDI Track Chunk");

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
				{ } s when s.SequenceEqual("MThd"u8) => MidiHeaderTag,
				{ } s when s.SequenceEqual("MTrk"u8) => MidiTrackTag,
				_ => new StructureTag("MIDI Unknown Chunk")
			}));
		}
		return builder.ToImmutable();
	}
}
