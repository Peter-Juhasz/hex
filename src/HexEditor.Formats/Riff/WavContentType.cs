using HexEditor.Core.ContentType;
using HexEditor.Formats.Text;
using HexEditor.Model;

namespace HexEditor.Formats.Riff;

public class WavContentTypeDefinition() : ContentTypeDefinition(Id, baseType: BinaryContentTypeDefinition.Id)
{
	public const string Id = "wav";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinarySnapshot source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".wav"]));
}
