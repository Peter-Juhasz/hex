using HexEditor.Core.ContentType;
using HexEditor.Model;

namespace HexEditor.Formats.Text;

public class BinaryContentTypeDefinition() : ContentTypeDefinition(Id)
{
	public const string Id = "binary";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinarySnapshot source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".bin"]));
}
