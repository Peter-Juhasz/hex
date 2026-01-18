using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiStructureTagger : ITagger<StructureTag>
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

		long startOffset = 0;
		byte[] buffer = new byte[8];
		using var _ = ImmutableArrayBuilderPool<TagSpan<StructureTag>>.GetPooledObject(out var builder);
		while (startOffset < span.Span.EndOffset)
		{
			// try read chunk header
			await snapshot.CopyToAsync(startOffset, buffer, cancellationToken).ConfigureAwait(false);

			// parse
			if (!TryParseChunkHeader(buffer, out var type, out var length))
			{
				break;
			}

			var tag = 
				type.SequenceEqual("MThd"u8) ? MidiHeaderTag :
				type.SequenceEqual("MTrk"u8) ? MidiTrackTag : 
				null;
			if (tag == null)
			{
				break;
			}

			// add span
			var fullExtent = new LongSpan(startOffset, 8 + length);
			if (fullExtent.IntersectsWith(span.Span))
			{
				builder.Add(new TagSpan<StructureTag>(
					Span: new(snapshot, fullExtent),
					Tag: tag
				));
			}
			else if (span.Span.EndOffset < fullExtent.StartOffset)
			{
				break;
			}

			// advance
			startOffset += fullExtent.Length;
		}

		return builder.ToImmutable();
	}

	private static bool TryParseChunkHeader(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> type, out int length)
	{
		type = bytes[..4];
		length = BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]);
		return true;
	}
}
