using HexEditor.Composition;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiDiagnostics(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : AbstractSyntaxTreeTagger<DiagnosticTag>(syntaxTreeProvider)
{
	protected override ImmutableArray<TagSpan<DiagnosticTag>> GetTags(IPartialSyntaxTree syntaxTree, SnapshotSpan span, CancellationToken cancellationToken)
	{
		if (syntaxTree.Root is not SyntaxNodeList list)
		{
			return [];
		}

		using var builder = new PooledArrayBuilder<TagSpan<DiagnosticTag>>();
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
			if (!isKnownChunk)
			{
				builder.Add(new TagSpan<DiagnosticTag>(chunkNode.TypeToken.Span, new DiagnosticTag(BinaryDiagnostics.UnknownChunkHeader)));
			}

			if (chunkNode.Span.Start.Position + 8 + chunkNode.LengthToken.Value != chunkNode.Span.End.Position)
			{
				builder.Add(new TagSpan<DiagnosticTag>(chunkNode.LengthToken.Span, new DiagnosticTag(BinaryDiagnostics.InvalidChunkLength)));
			}
		}
		return builder.ToImmutableArray();
	}
}
