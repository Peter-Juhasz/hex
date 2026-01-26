using HexEditor.Composition;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavDiagnostics(
	[FromKeyedServices(WavContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<DiagnosticTag>
{
	private static readonly DiagnosticDescriptor UnknownChunkDiagnostic = new(
		Id: "WAV001",
		Title: "Unknown WAV Chunk",
		MessageFormat: "The chunk type '{0}' is not recognized.",
		Severity: DiagnosticSeverity.Warning
	);

	private static readonly DiagnosticDescriptor InvalidLengthDiagnostic = new(
		Id: "WAV002",
		Title: "Invalid WAV Chunk Length",
		MessageFormat: "The chunk length specified is invalid.",
		Severity: DiagnosticSeverity.Error
	);

	public async Task<ImmutableArray<TagSpan<DiagnosticTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
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

		using var _ = ImmutableArrayBuilderPool<TagSpan<DiagnosticTag>>.GetPooledObject(out var builder);
		foreach (var child in list.Children)
		{
			if (child is not TypeLengthChunkSyntaxNode chunkNode)
			{
				continue;
			}

			var isKnownChunk = chunkNode.TypeToken.Data.Span switch
			{
				{ } s when s.SequenceEqual("RIFF"u8) => true,
				{ } s when s.SequenceEqual("fmt "u8) => true,
				{ } s when s.SequenceEqual("fact"u8) => true,
				{ } s when s.SequenceEqual("data"u8) => true,
				_ => false
			};
			if (!isKnownChunk)
			{
				builder.Add(new TagSpan<DiagnosticTag>(chunkNode.TypeToken.Span, new DiagnosticTag(UnknownChunkDiagnostic)));
			}

			if (chunkNode.Span.Start.Position + 8 + chunkNode.LengthToken.Value != chunkNode.Span.End.Position)
			{
				builder.Add(new TagSpan<DiagnosticTag>(chunkNode.LengthToken.Span, new DiagnosticTag(InvalidLengthDiagnostic)));
			}
		}
		return builder.ToImmutable();
	}
}
