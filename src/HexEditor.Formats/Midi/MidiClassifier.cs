using HexEditor.Composition;
using HexEditor.Core.Syntax;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiClassifier(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : BinaryChunkClassificationTagger(syntaxTreeProvider)
{
}
