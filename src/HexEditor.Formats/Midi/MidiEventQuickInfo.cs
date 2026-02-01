using HexEditor.Composition;
using HexEditor.Core.Model;
using HexEditor.Core.QuickInfo;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiEventQuickInfo(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) 
	: AbstractQuickInfoTagger
{
	protected override async Task<ImmutableArray<TagSpan<QuickInfoTag>>> GetTagsAsync(SnapshotPoint triggerPoint, CancellationToken cancellationToken)
	{
		var syntaxTree = await syntaxTreeProvider.GetSyntaxTreeAsync(SnapshotSpan.Create(triggerPoint, 0), cancellationToken).ConfigureAwait(false);
		if (syntaxTree == null)
		{
			goto NoResults;
		}

		// find event
		if (await MidiParser.TryFindEventAsync(syntaxTree, triggerPoint, cancellationToken).ConfigureAwait(false) is not { } eventSpan)
		{
			goto NoResults;
		}

		var buffer = new byte[eventSpan.Length];
		await eventSpan.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
		if (!MidiParser.TryReadVariableLengthQuantity(buffer, out _, out var deltaTimeLength))
		{
			goto NoResults;
		}

		// check if trigger point is inside status
		var statusSpan = SnapshotSpan.Create(
			eventSpan.Start + deltaTimeLength,
			buffer[deltaTimeLength] switch
			{
				0xFF => 2, // meta event
				_ => 1 // midi event
			}
		);
		if (!statusSpan.Contains(triggerPoint))
		{
			goto NoResults;
		}

		int currentStatus = buffer[deltaTimeLength];
		if (MidiEventNode.IsMetaEvent((byte)currentStatus))
		{
			currentStatus = 0xFF00 + buffer[deltaTimeLength + 1];
		}

		return [new(statusSpan, new TextQuickInfoTag(String.Join(Environment.NewLine,
		[
			currentStatus.ToString(currentStatus >= 0xFF ? "X4" : "X2"),
			"MIDI Event"
		])))];

	NoResults:
		return [];
	}
}
