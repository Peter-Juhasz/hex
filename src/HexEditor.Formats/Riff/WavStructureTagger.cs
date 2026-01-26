using HexEditor.Composition;
using HexEditor.Core.Structure;
using HexEditor.Core.Syntax;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavStructureTagger(
	[FromKeyedServices(WavContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : BinaryChunkStructureTagger(syntaxTreeProvider)
{
	private static readonly StructureTag WavRiffTag = new("WAV RIFF Chunk");
	private static readonly StructureTag WavFormatTag = new("WAV Format Chunk");
	private static readonly StructureTag WavDataTag = new("WAV Data Chunk");
	private static readonly StructureTag WavFactTag = new("WAV Fact Chunk");

	protected override StructureTag GetTag(ReadOnlySpan<byte> type) => type switch
	{
		{ } s when s.SequenceEqual("RIFF"u8) => WavRiffTag,
		{ } s when s.SequenceEqual("fmt "u8) => WavFormatTag,
		{ } s when s.SequenceEqual("fact"u8) => WavFactTag,
		{ } s when s.SequenceEqual("data"u8) => WavDataTag,
		_ => new StructureTag($"WAV {Encoding.ASCII.GetString(type)} Chunk")
	};
}
