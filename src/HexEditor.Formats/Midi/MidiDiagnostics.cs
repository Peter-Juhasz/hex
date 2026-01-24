using HexEditor.Composition;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiDiagnostics(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : ITagger<DiagnosticTag>
{
	public async Task<ImmutableArray<TagSpan<DiagnosticTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
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

		using var _ = ImmutableArrayBuilderPool<TagSpan<DiagnosticTag>>.GetPooledObject(out var builder);
		builder.Add(new TagSpan<DiagnosticTag>(snapshot.Span.Slice(4, 4), new DiagnosticTag(new DiagnosticDescriptor("ID", DiagnosticSeverity.Error, "Sample message"))));
		return builder.ToImmutable();
	}
}
