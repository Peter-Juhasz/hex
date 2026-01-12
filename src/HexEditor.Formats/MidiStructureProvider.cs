using HexEditor.Model;
using HexEditor.Structure;
using HexEditor.ViewModel;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

internal sealed class MidiStructureProvider : IStructureProvider
{
	public ValueTask<ImmutableArray<StructureSpan>> GetStructureSpansAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken)
	{
		if (!buffer.TryRead(new MemorySpan(0, (int)span.EndOffset), out var data)) // we do not expect MIDI files larger than 2GB
		{
			return new([]);
		}

		var startOffset = 0;
		var remainingBytes = data.Span;
		using var list = new PooledArrayBuilder<StructureSpan>();
		while (TryReadChunk(remainingBytes, out var type, out var length))
		{
			var fullExtent = 8 + length;
			var spanSpan = new MemorySpan(startOffset, fullExtent);

			if (span.IntersectsWith(spanSpan))
			{
				list.Add(new StructureSpan(
					Span: spanSpan,
					Label:
						type == "MThd"u8 ? "MIDI Header Chunk" :
						type == "MTrk"u8 ? "MIDI Track Chunk" :
						Encoding.ASCII.GetString(type)
				));
			}

			startOffset += fullExtent;
			remainingBytes = remainingBytes[fullExtent..];
		}

		return new(list.ToImmutableArray());
	}

	private static bool TryReadChunk(ReadOnlySpan<byte> bytes, out ReadOnlySpan<byte> type, out int length)
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
