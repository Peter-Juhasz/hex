using HexEditor.Core.ContentType;
using HexEditor.Formats.Text;
using HexEditor.Model;

namespace HexEditor.Formats.Riff;

public class RiffContentTypeDefinition() : ContentTypeDefinition(Id, baseType: BinaryContentTypeDefinition.Id)
{
	public const string Id = "riff";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinarySnapshot source, CancellationToken cancellationToken) =>
		new(false);
}
