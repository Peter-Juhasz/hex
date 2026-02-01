using HexEditor.Composition;
using HexEditor.Core.Fields;
using HexEditor.Core.Model;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiEventFieldTagger(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider,
	IViewAccessor viewAccessor
) : AbstractFieldTagger(viewAccessor)
{
	protected override async Task<ImmutableArray<TagSpan<FieldTag>>> GetTagsAsync(SnapshotPoint triggerPoint, SnapshotSpan span, CancellationToken cancellationToken)
	{
		var syntaxTree = await syntaxTreeProvider.GetSyntaxTreeAsync(span, cancellationToken).ConfigureAwait(false);
		if (syntaxTree == null)
		{
			goto NoResults;
		}

		// find event
		if (await MidiParser.TryFindEventAsync(syntaxTree, triggerPoint, cancellationToken).ConfigureAwait(false) is not { } eventSpan)
		{
			goto NoResults;
		}

		if (eventSpan.Contains(triggerPoint))
		{
			return [new(eventSpan, FieldTag.Instance)];
		}

	NoResults:
		return [];
	}
}
