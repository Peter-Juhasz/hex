using HexEditor.Composition;
using HexEditor.Core.Syntax;
using HexEditor.Core.ViewModel;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace HexEditor.Formats.Midi;

[ContentType(MidiContentTypeDefinition.Id)]
public sealed class MidiChunkReferenceHighlighter(
	[FromKeyedServices(MidiContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider,
	IViewAccessor viewAccessor
) : BinaryChunkReferenceTagger(viewAccessor, syntaxTreeProvider)
{
}
