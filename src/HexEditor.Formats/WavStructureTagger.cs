using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

public sealed class WavStructureTagger : ITagger<StructureTag>
{
	private static readonly StructureTag RiffChunkTag = new("RIFF Chunk");
	private static readonly StructureTag FormatChunkTag = new("Format Chunk");
	private static readonly StructureTag DataChunkTag = new("Data Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		long startOffset = 0;
		byte[] headerBytes = new byte[8];
		var list = new List<TagSpan<StructureTag>>();
		
		// Check if this is a RIFF/WAVE file
		await snapshot.CopyToAsync(startOffset, headerBytes, cancellationToken);
		if (!TryParseChunkHeader(headerBytes, out var firstType, out var firstLength) || 
		    !firstType.SequenceEqual("RIFF"u8))
		{
			return ImmutableArray<TagSpan<StructureTag>>.Empty;
		}

		// Validate that firstLength doesn't cause overflow
		if (firstLength > int.MaxValue - 8)
		{
			return ImmutableArray<TagSpan<StructureTag>>.Empty;
		}

		// Read WAVE identifier (4 bytes after RIFF header)
		byte[] waveIdBytes = new byte[4];
		await snapshot.CopyToAsync(startOffset + 8, waveIdBytes, cancellationToken);
		if (!waveIdBytes.AsSpan().SequenceEqual("WAVE"u8))
		{
			return ImmutableArray<TagSpan<StructureTag>>.Empty;
		}

		// Add RIFF chunk (includes the WAVE identifier in its size)
		var riffExtent = new LongSpan(startOffset, 8 + firstLength);
		if (riffExtent.IntersectsWith(span.Span))
		{
			list.Add(new TagSpan<StructureTag>(
				Span: new(snapshot, riffExtent),
				Tag: RiffChunkTag
			));
		}

		// Parse sub-chunks starting after "WAVE" identifier
		startOffset = 12; // Skip RIFF header (8 bytes) + WAVE identifier (4 bytes)
		while (startOffset < span.Span.EndOffset && startOffset < riffExtent.EndOffset && snapshot.Length >= 8 && startOffset <= snapshot.Length - 8)
		{
			// try read chunk header
			await snapshot.CopyToAsync(startOffset, headerBytes, cancellationToken);

			// parse
			if (!TryParseChunkHeader(headerBytes, out var type, out var length))
			{
				break;
			}

			// Validate that length doesn't cause overflow when adding header size
			if (length > int.MaxValue - 8)
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
						type.SequenceEqual("fmt "u8) ? FormatChunkTag :
						type.SequenceEqual("data"u8) ? DataChunkTag :
						new StructureTag(Encoding.ASCII.GetString(type).TrimEnd())
				));
			}

			// advance (align to even byte boundary as per RIFF spec)
			startOffset += fullExtent.Length;
			if (length % 2 == 1)
			{
				startOffset++;
			}
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
		length = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
		
		// Validate length is non-negative
		if (length < 0)
		{
			type = default;
			length = 0;
			return false;
		}
		
		return true;
	}
}
