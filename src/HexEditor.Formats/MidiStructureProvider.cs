using HexEditor.Model;
using HexEditor.Structure;
using HexEditor.ViewModel;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

internal sealed class MidiStructureProvider : IStructureProvider
{
	public async ValueTask<ImmutableArray<StructureSpan>> GetStructureSpansAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken)
	{
		long startOffset = 0;
		byte[] headerBytes = new byte[8];
		var list = new List<StructureSpan>();
		while (startOffset < span.EndOffset)
		{
			// try read chunk header
			if (!buffer.TryCopyTo(startOffset, headerBytes))
			{
				await buffer.DataBuffer.CopyToAsync(new(startOffset, headerBytes.Length), headerBytes, cancellationToken);
			}

			// parse
			if (!TryParseChunkHeader(headerBytes, out var type, out var length))
			{
				break;
			}

			// add span
			var fullExtent = new MemorySpan(startOffset, 8 + length);
			list.Add(new StructureSpan(
				Span: fullExtent,
				Label:
					type == "MThd"u8 ? "MIDI Header Chunk" :
					type == "MTrk"u8 ? "MIDI Track Chunk" :
					Encoding.ASCII.GetString(type)
			));

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
