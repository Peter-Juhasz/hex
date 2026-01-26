using HexEditor.Composition;
using HexEditor.Core.Syntax;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace HexEditor.Formats.Riff;

[ContentType(WavContentTypeDefinition.Id)]
public sealed class WavClassifier(
	[FromKeyedServices(WavContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider
) : BinaryChunkClassificationTagger(syntaxTreeProvider)
{
}
