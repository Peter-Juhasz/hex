using HexEditor.Composition;
using HexEditor.Core.Syntax;
using HexEditor.Core.ViewModel;
using HexEditor.Formats.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace HexEditor.Formats.Riff;

[ContentType(RiffContentTypeDefinition.Id)]
public sealed class RiffChunkReferenceHighlighter(
	[FromKeyedServices(RiffContentTypeDefinition.Id)] IPartialSyntaxTreeProvider syntaxTreeProvider,
	IViewAccessor viewAccessor
) : BinaryChunkReferenceTagger(viewAccessor, syntaxTreeProvider)
{
}
