using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

public sealed class WavStructureTagger : ITagger<StructureTag>
{
	private static readonly StructureTag RiffHeaderTag = new("RIFF Header");
	private static readonly StructureTag FormatChunkTag = new("Format Chunk");
	private static readonly StructureTag DataChunkTag = new("Data Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		long startOffset = 0;
		byte[] headerBytes = new byte[12];
		var list = new List<TagSpan<StructureTag>>();

		// try read RIFF header (12 bytes: "RIFF" + size + "WAVE")
		await snapshot.CopyToAsync(startOffset, headerBytes, cancellationToken);

		// validate RIFF header
		if (!TryParseRiffHeader(headerBytes, out var riffSize))
		{
			return list.ToImmutableArray();
		}

		// add RIFF header span (first 12 bytes)
		var riffHeaderExtent = new LongSpan(startOffset, 12);
		if (riffHeaderExtent.IntersectsWith(span.Span))
		{
			list.Add(new TagSpan<StructureTag>(
				Span: new(snapshot, riffHeaderExtent),
				Tag: RiffHeaderTag
			));
		}

		// move past RIFF header to sub-chunks
		startOffset = 12;
		byte[] chunkHeaderBytes = new byte[8];

		while (startOffset < span.Span.EndOffset && startOffset < 12 + riffSize)
		{
			// try read chunk header
			await snapshot.CopyToAsync(startOffset, chunkHeaderBytes, cancellationToken);

			// parse
			if (!TryParseChunkHeader(chunkHeaderBytes, out var type, out var length))
			{
				break;
			}

			// calculate actual chunk size (header + data, with word alignment padding)
			long chunkDataSize = length;
			long fullChunkSize = 8 + chunkDataSize;

			// add span
			var fullExtent = new LongSpan(startOffset, fullChunkSize);
			if (fullExtent.IntersectsWith(span.Span))
			{
				list.Add(new TagSpan<StructureTag>(
					Span: new(snapshot, fullExtent),
					Tag:
						type.SequenceEqual("fmt "u8) ? FormatChunkTag :
						type.SequenceEqual("data"u8) ? DataChunkTag :
						new StructureTag(Encoding.ASCII.GetString(type) + " Chunk")
				));
			}

			// advance (chunks are word-aligned, so add padding byte if length is odd)
			startOffset += fullChunkSize + (chunkDataSize % 2);
		}

		return list.ToImmutableArray();
	}

	private static bool TryParseRiffHeader(ReadOnlySpan<byte> bytes, out int riffSize)
	{
		riffSize = 0;

		if (bytes.Length < 12)
		{
			return false;
		}

		// Check "RIFF" magic
		if (!bytes[..4].SequenceEqual("RIFF"u8))
		{
			return false;
		}

		// Check "WAVE" format
		if (!bytes[8..12].SequenceEqual("WAVE"u8))
		{
			return false;
		}

		riffSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
		return true;
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
		length = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
		return true;
	}
}
