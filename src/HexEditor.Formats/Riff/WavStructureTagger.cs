using HexEditor.Core.ContentType;
using HexEditor.Core.Structure;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavStructureTagger : ITagger<StructureTag>
{
	private static readonly StructureTag WavRiffTag = new("WAV RIFF Chunk");
	private static readonly StructureTag WavFormatTag = new("WAV Format Chunk");
	private static readonly StructureTag WavDataTag = new("WAV Data Chunk");
	private static readonly StructureTag WavFactTag = new("WAV Fact Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length < 8)
		{
			return [];
		}

		long startOffset;
		byte[] buffer = new byte[8];

		// try read RIFF header
		var riffChunk = await ReadChunkAsync(snapshot.Span, buffer, cancellationToken).ConfigureAwait(false);
		if (riffChunk == null || !buffer.AsSpan(0, 4).SequenceEqual("RIFF"u8))
		{
			return [];
		}

		using var _ = ImmutableArrayBuilderPool<TagSpan<StructureTag>>.GetPooledObject(out var builder);
		var fullExtent = new LongSpan(0, 8 + riffChunk.Value.Size);
		if (span.Span.IntersectsWith(fullExtent))
		{
			builder.Add(new TagSpan<StructureTag>(
				Span: new(snapshot, fullExtent),
				Tag: WavRiffTag
			));
		}
		startOffset = 8 + 4;

		if (span.Span.EndOffset < startOffset )
		{
			return builder.ToImmutable();
		}

		// try read chunks
		while (await ReadChunkAsync(snapshot.Slice(startOffset), buffer, cancellationToken).ConfigureAwait(false) is Chunk chunk)
		{
			var chunkId = buffer.AsSpan(0, 4);
			fullExtent = new LongSpan(startOffset, 8 + chunk.Size);
			if (span.Span.IntersectsWith(fullExtent))
			{
				var tag = chunkId switch
				{
					_ when chunkId.SequenceEqual("fmt "u8) => WavFormatTag,
					_ when chunkId.SequenceEqual("fact"u8) => WavFactTag,
					_ when chunkId.SequenceEqual("data"u8) => WavDataTag,
					_ => new StructureTag($"WAV {Encoding.ASCII.GetString(chunkId)} Chunk"),
				};
				builder.Add(new TagSpan<StructureTag>(
					Span: new(snapshot, fullExtent),
					Tag: tag
				));
			}
			startOffset = fullExtent.EndOffset;
			if (span.Span.EndOffset < startOffset)
			{
				break;
			}
		}

		return builder.ToImmutable();
	}

	private static bool TryParseChunkHeader(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> chunkId, out int chunkSize)
	{
		chunkId = bytes[..4];
		chunkSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
		return true;
	}

	private async static Task<Chunk?> ReadChunkAsync(SnapshotSpan remaining, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (remaining.Span.Length < 8)
		{
			return null;
		}

		var headerSpan = remaining.Slice(0, 8);
		await headerSpan.Snapshot.CopyToAsync(headerSpan.Span.StartOffset, buffer, cancellationToken).ConfigureAwait(false);

		if (!TryParseChunkHeader(buffer.Span, out var type, out var length))
		{
			return null;
		}

		return new Chunk(length);
	}

	private record struct Chunk(int Size);
}
