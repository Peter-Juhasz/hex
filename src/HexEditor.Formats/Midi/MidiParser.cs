using HexEditor.Composition;
using HexEditor.Core.ContentType;
using HexEditor.Core.Syntax;
using HexEditor.Model;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiParser : IPartialSyntaxTreeFactory
{
	public async ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, IPartialSyntaxTree? before, CancellationToken cancellationToken)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length < 8)
		{
			return null;
		}

		long startOffset = 0;
		byte[] buffer = new byte[8];
		using var _ = ImmutableArrayBuilderPool<SyntaxNode>.GetPooledObject(out var builder);
		while (startOffset < span.Span.EndOffset)
		{
			// try read chunk header
			await snapshot.CopyToAsync(startOffset, buffer, cancellationToken).ConfigureAwait(false);

			// parse
			if (!TryParseChunkHeader(buffer, out var type, out var length))
			{
				break;
			}

			// add span
			var fullExtent = new LongSpan(startOffset, 8 + length);
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
		length = BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]);
		return true;
	}
}
