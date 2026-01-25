using HexEditor.Composition;
using HexEditor.Core.ContentType;
using HexEditor.Core.Syntax;
using HexEditor.Model;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavParser : IPartialSyntaxTreeFactory
{
	public async ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, IPartialSyntaxTree? before, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length < 12)
		{
			return null;
		}

		byte[] buffer = new byte[8];
		using var _ = ImmutableArrayBuilderPool<SyntaxNode>.GetPooledObject(out var builder);

		// Read RIFF header
		long startOffset = 0;
		await snapshot.CopyToAsync(startOffset, buffer, cancellationToken).ConfigureAwait(false);

		if (!TryParseChunkHeader(buffer, out var type, out var length))
		{
			return null;
		}

		// Verify this is a RIFF chunk
		if (!type.SequenceEqual("RIFF"u8))
		{
			return null;
		}

		// Store type bytes before await
		byte[] typeBytes = type.ToArray();

		// Verify WAVE format
		byte[] waveBuffer = new byte[4];
		await snapshot.CopyToAsync(8, waveBuffer, cancellationToken).ConfigureAwait(false);
		if (!waveBuffer.AsSpan().SequenceEqual("WAVE"u8))
		{
			return null;
		}

		// Add RIFF chunk
		var fullExtent = new LongSpan(startOffset, 8 + length);
		if (fullExtent.IntersectsWith(span.Span))
		{
			var fullSpan = new SnapshotSpan(snapshot, fullExtent);
			var typeSpan = fullSpan.Slice(0, 4);
			var lengthSpan = fullSpan.Slice(4, 4);
			builder.Add(new TypeLengthChunkSyntaxNode(
				Span: fullSpan,
				TypeToken: new SyntaxToken(typeSpan, typeBytes),
				LengthToken: new Int32SyntaxToken(lengthSpan, length)
			));
		}

		// Skip RIFF header (4 bytes type + 4 bytes length) and WAVE format identifier (4 bytes)
		startOffset = 12;

		// Read chunks inside RIFF
		while (startOffset < snapshot.Length && startOffset < span.Span.EndOffset)
		{
			if (startOffset + 8 > snapshot.Length)
			{
				break;
			}

			// try read chunk header
			await snapshot.CopyToAsync(startOffset, buffer, cancellationToken).ConfigureAwait(false);

			// parse
			if (!TryParseChunkHeader(buffer, out type, out length))
			{
				break;
			}

			// add span
			fullExtent = new LongSpan(startOffset, 8 + length);
			if (fullExtent.IntersectsWith(span.Span))
			{
				var fullSpan = new SnapshotSpan(snapshot, fullExtent);
				var typeSpan = fullSpan.Slice(0, 4);
				var lengthSpan = fullSpan.Slice(4, 4);
				builder.Add(new TypeLengthChunkSyntaxNode(
					Span: fullSpan,
					TypeToken: new SyntaxToken(typeSpan, type.ToArray()),
					LengthToken: new Int32SyntaxToken(lengthSpan, length)
				));
			}
			else if (span.Span.EndOffset < fullExtent.StartOffset)
			{
				break;
			}

			// advance
			startOffset += fullExtent.Length;
		}

		return new PartialSyntaxTree(
			new SyntaxNodeList(ImmutableList.CreateRange(builder.ToImmutable()))
		);
	}

	private static bool TryParseChunkHeader(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> type, out int length)
	{
		type = bytes[..4];
		length = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
		return true;
	}
}
