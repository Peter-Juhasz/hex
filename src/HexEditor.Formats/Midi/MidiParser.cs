using HexEditor.Composition;
using HexEditor.Core.Model;
using HexEditor.Core.Syntax;
using HexEditor.Formats.Binary;
using HexEditor.Model;
using System.Buffers.Binary;

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
		using var _ = ImmutableListBuilderPool<SyntaxNode>.GetPooledObject(out var builder);
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
			new SyntaxNodeList(builder.ToImmutable())
		);
	}

	private static bool TryParseChunkHeader(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> type, out int length)
	{
		type = bytes[..4];
		length = BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]);
		return true;
	}

	internal static bool TryReadMidiEvent(ReadOnlySpan<byte> data, ref byte? runningStatus, out int deltaTimeLength, out int fullLength)
	{
		if (!TryReadVariableLengthQuantity(data, out _, out deltaTimeLength))
		{
			fullLength = 0;
			return false;
		}

		data = data[deltaTimeLength..];

		var status = data[0];
		switch (status)
		{
			// Meta Event
			case 0xFF:
				{
					data = data[1..]; // skip status byte
					data = data[1..]; // skip meta type byte
					if (!TryReadVariableLengthQuantity(data, out int quantity, out int vlqLength)) // read length
					{
						fullLength = 0;
						return false;
					}
					fullLength = deltaTimeLength + 1 + 1 + vlqLength + quantity;
					return true;
				}

			// System Exclusive Event
			case 0xF0 or 0xF7:
				{
					data = data[1..]; // skip status byte
					if (!TryReadVariableLengthQuantity(data, out int quantity, out int vlqLength)) // read length
					{
						fullLength = 0;
						return false;
					}
					fullLength = deltaTimeLength + 1 + vlqLength + quantity;
					return true;
				}

			default:
				{
					// running status, reuse the last status byte
					if ((status & 0x80) == 0)
					{
						if (runningStatus == null)
						{
							fullLength = 0;
							return false;
						}

						status = runningStatus.Value;
					}

					var length = MidiEventNode.GetParameterCount(status);
					if (length < 0)
					{
						fullLength = 0;
						return false;
					}

					runningStatus = status;
					fullLength = deltaTimeLength + 1 + length;
					return true;
				}
		}
	}

	internal static bool TryReadVariableLengthQuantity(ReadOnlySpan<byte> data, out int quantity, out int length)
	{
		if (data.IsEmpty)
		{
			quantity = 0;
			length = 0;
			return false;
		}

		int result = 0;
		int bytesRead = 0;

		foreach (var b in data)
		{
			// Shift existing 7 bits and add the lower 7 bits of this byte.
			// Check for potential overflow before shifting.
			if (result > (int.MaxValue >> 7))
			{
				quantity = 0;
				length = 0;
				return false;
			}

			result = (result << 7) | (b & 0x7F);
			bytesRead++;

			// If MSB is clear, this is the last byte of the VLQ.
			if ((b & 0x80) == 0)
			{
				data = data[bytesRead..];
				quantity = result;
				length = bytesRead;
				return true;
			}

			// MIDI VLQs are at most 4 bytes (28 bits of payload).
			if (bytesRead == 4)
			{
				quantity = 0;
				length = bytesRead;
				return false;
			}
		}

		// Ran out of data while continuation bit was still set.
		quantity = 0;
		length = 0;
		return false;
	}

	public static async Task<SnapshotSpan?> TryFindEventAsync(IPartialSyntaxTree syntaxTree, SnapshotPoint triggerPoint, CancellationToken cancellationToken)
	{
		// find track chunk containing the trigger point
		if (syntaxTree.Root.DescendantsAndSelf<TypeLengthChunkSyntaxNode>().FirstOrDefault(n =>
			n.TypeToken.Data.Span.SequenceEqual("MTrk"u8) &&
			n.Span.Contains(triggerPoint)
		) is not { } trackNode)
		{
			return null;
		}

		// read track data
		// TODO: optimize to avoid allocation
		var trackBuffer = new byte[trackNode.Span.Length - 8];
		await trackNode.Span.Slice(8).CopyToAsync(trackBuffer, cancellationToken).ConfigureAwait(false);

		// find current event
		var runningStatus = (byte?)null;
		var startIndex = 0;
		while (MidiParser.TryReadMidiEvent(trackBuffer.AsSpan(startIndex), ref runningStatus, out int deltaTimeLength, out int fullLength))
		{
			var eventSpan = SnapshotSpan.Create(
				trackNode.Span.Start + 8 + startIndex,
				fullLength
			);
			if (eventSpan.Contains(triggerPoint))
			{
				return eventSpan;
			}
			else if (triggerPoint < eventSpan.Start)
			{
				return null;
			}

			startIndex += fullLength;
		}

		return null;
	}
}

public readonly record struct MidiEventNode(
	SnapshotSpan Span,
	byte TimeLength,
	byte Status
)
{
	public static bool IsMetaEvent(byte status) => status == 0xFF;

	public static int GetParameterCount(byte status)
	{
		var messageType = status & 0xF0;
		return messageType switch
		{
			// system message
			0xF0 => -1,

			// reserved
			0xF4 or 0xF5 or 0xF9 or 0xFD => 0,

			// note off, note on, polyphonic key pressure, control change, pitch bend
			<= 0xAF => 2,

			// control messages
			>= 0xB0 and <= 0xBF => 2,

			// program change, channel pressure
			>= 0xC0 and <= 0xDF => 1,

			// pitch bend
			>= 0xE0 and <= 0xEF => 2,

			// song position pointer
			0xF2 => 2,

			// song select
			0xF3 => 1,

			// tune request
			0xF6 => 0,

			// end of exclusive
			0xF7 => 0,

			// timing clock
			0xF8 => 0,

			// start
			0xFA => 0,
			// continue
			0xFB => 0,
			// stop
			0xFC => 0,

			// active sensing
			0xFE => 0,
			// system reset
			0xFF => 0,

			_ => throw new NotImplementedException($"MIDI event parameter {status} count not implemented."),
		};
	}
}
