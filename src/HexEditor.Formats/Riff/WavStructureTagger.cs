using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavStructureTagger : ITagger<StructureTag>
{
	private static readonly StructureTag WavRiffTag = new("WAV RIFF Chunk");
	private static readonly StructureTag WavFormatTag = new("WAV Format Chunk");
	private static readonly StructureTag WavDataTag = new("WAV Data Chunk");

	public async Task<ImmutableArray<TagSpan<StructureTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length < 8)
		{
			return [];
		}

		long startOffset;
		byte[] buffer = new byte[8];
		using var _ = ImmutableArrayBuilderPool<TagSpan<StructureTag>>.GetPooledObject(out var builder);

		// try read RIFF header
		var riffChunk = await ReadChunkAsync(snapshot.Span, buffer, cancellationToken);
		if (riffChunk == null || !buffer.SequenceEqual("RIFF"u8))
		{
			return [];
		}
		builder.Add(new TagSpan<StructureTag>(
			Span: new(snapshot, new LongSpan(0, 8 + riffChunk.Value.Size)),
			Tag: WavRiffTag
		));
		startOffset = 8 + 4;

		// try read format chunk
		var formatChunk = await ReadChunkAsync(snapshot.Slice(startOffset), buffer, cancellationToken);
		var fullExtent = new LongSpan(startOffset, 8 + riffChunk.Value.Size);
		if (formatChunk == null || !buffer.SequenceEqual("fmt "u8) || !span.Span.IntersectsWith(fullExtent))
		{
			return builder.ToImmutable();
		}

		builder.Add(new TagSpan<StructureTag>(
			Span: new(snapshot, fullExtent),
			Tag: WavFormatTag
		));
		startOffset = fullExtent.EndOffset;

		// try read data chunk
		var dataChunk = await ReadChunkAsync(snapshot.Slice(startOffset), buffer, cancellationToken);
		fullExtent = new LongSpan(startOffset, 8 + riffChunk.Value.Size);
		if (dataChunk == null || !buffer.SequenceEqual("data"u8) || !span.Span.IntersectsWith(fullExtent))
		{
			return builder.ToImmutable();
		}
		builder.Add(new TagSpan<StructureTag>(
			Span: new(snapshot, fullExtent),
			Tag: WavDataTag
		));
		startOffset = fullExtent.EndOffset;

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
		await headerSpan.Snapshot.CopyToAsync(headerSpan.Span.StartOffset, buffer, cancellationToken);

		if (!TryParseChunkHeader(buffer.Span, out var type, out var length))
		{
			return null;
		}

		return new Chunk(length);
	}

	private record struct Chunk(int Size);
}
