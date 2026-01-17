using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

public sealed class MidiStructureTagger : ITagger<StructureTag>
{
	private static readonly StructureTag MidiHeaderTag = new("MIDI Header Chunk");
	private static readonly StructureTag MidiTrackTag = new("MIDI Track Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		long startOffset = 0;
		byte[] headerBytes = new byte[8];
		var list = new List<TagSpan<StructureTag>>();
		while (startOffset < span.Span.EndOffset)
		{
			// try read chunk header
			await snapshot.CopyToAsync(startOffset, headerBytes, cancellationToken);

			// parse
			if (!TryParseChunkHeader(headerBytes, out var type, out var length))
			{
				break;
			}

			// add span
			var fullExtent = new LongSpan(startOffset, 8 + length);
			if (fullExtent.IntersectsWith(span.Span))
			{
				list.Add(new TagSpan<StructureTag>(
					Span: new(snapshot, fullExtent),
					Tag:
						type.SequenceEqual("MThd"u8) ? MidiHeaderTag :
						type.SequenceEqual("MTrk"u8) ? MidiTrackTag :
						new StructureTag(Encoding.ASCII.GetString(type))
				));
			}

			// advance
			startOffset += fullExtent.Length;
		}

		return list.ToImmutableArray();
	}

	private static bool TryParseChunkHeader(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> type, out int length)
	{
		if (bytes.Length < 8)
		{
			type = default;
			length = 0;
			return false;
		}

		type = bytes[..4];
		length = BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]);
		return true;
	}
}
