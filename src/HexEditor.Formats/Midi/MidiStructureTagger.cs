using HexEditor.Composition;
using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiStructureTagger(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : BinaryChunkStructureTagger(syntaxTreeProvider)
{
	private static readonly StructureTag MidiHeaderTag = new("MIDI Header Chunk");
	private static readonly StructureTag MidiTrackTag = new("MIDI Track Chunk");

	protected override StructureTag GetTag(ReadOnlySpan<byte> type) => type switch
	{
		{ } s when s.SequenceEqual("MThd"u8) => MidiHeaderTag,
		{ } s when s.SequenceEqual("MTrk"u8) => MidiTrackTag,
		_ => new StructureTag("MIDI Unknown Chunk")
	};
}
