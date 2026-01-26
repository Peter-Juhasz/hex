using HexEditor.Core.ContentType;
using HexEditor.Model;

namespace HexEditor.Formats.Text;

public class TextContentTypeDefinition() : ContentTypeDefinition(Id)
{
	public const string Id = "text";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinarySnapshot source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".txt"]));
}
