using HexEditor.Composition;
using HexEditor.Core.Model;
using HexEditor.Core.ReferenceHighlight;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiEventReferenceHighlighter(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider,
	IViewAccessor viewAccessor
) : AbstractReferenceTagger(viewAccessor)
{
	protected override async Task<ImmutableArray<TagSpan<ReferenceTag>>> GetTagsAsync(SnapshotPoint triggerPoint, SnapshotSpan span, CancellationToken cancellationToken)
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

		// find all matching events
		return await FindEventsAsync(syntaxTree, currentStatus, span, cancellationToken).ConfigureAwait(false);

	NoResults:
		return [];
	}

	private static async Task<ImmutableArray<TagSpan<ReferenceTag>>> FindEventsAsync(IPartialSyntaxTree syntaxTree, int currentStatus, SnapshotSpan span, CancellationToken cancellationToken)
	{
		using var _ = ImmutableArrayBuilderPool<TagSpan<ReferenceTag>>.GetPooledObject(out var tags);
		foreach (var trackNode in syntaxTree.Root.DescendantsAndSelf<TypeLengthChunkSyntaxNode>())
		{
			if (!trackNode.TypeToken.Data.Span.SequenceEqual("MTrk"u8))
			{
				continue;
			}

			if (span.End < trackNode.Span.Start)
			{
				break;
			}
			else if (span.Start > trackNode.Span.End)
			{
				continue;
			}

			var trackBuffer = new byte[trackNode.Span.Length - 8];
			await trackNode.Span.Slice(8).CopyToAsync(trackBuffer, cancellationToken).ConfigureAwait(false);

			var startIndex = 0;
			var runningStatus = (byte?)null;
			while (MidiParser.TryReadMidiEvent(trackBuffer.AsSpan(startIndex), ref runningStatus, out int deltaTimeLength, out int fullLength))
			{
				// calculate event span
				var eventSpan = SnapshotSpan.Create(
					trackNode.Span.Start + 8 + startIndex,
					fullLength
				);
				if (eventSpan.End < span.Start)
				{
					startIndex += fullLength;
					continue;
				}
				else if (eventSpan.End > span.End)
				{
					break;
				}

				// compare status
				int status = trackBuffer[startIndex + deltaTimeLength];
				if (MidiEventNode.IsMetaEvent((byte)status))
				{
					status = 0xFF00 + trackBuffer[startIndex + deltaTimeLength + 1];
				}
				if (status != currentStatus)
				{
					startIndex += fullLength;
					continue;
				}

				// add tag
				var statusSpan = SnapshotSpan.Create(
					eventSpan.Start + deltaTimeLength,
					trackBuffer[startIndex + deltaTimeLength] switch
					{
						0xFF => 2, // meta event
						_ => 1 // midi event
					}
				);
				tags.Add(new TagSpan<ReferenceTag>(statusSpan, ReferenceTag.ReferenceUsageTag));

				startIndex += fullLength;
			}
		}
		return tags.ToImmutable();
	}
}
