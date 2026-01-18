using HexEditor.Core.Model;
using HexEditor.Model;

namespace HexEditor.Formats.Riff;

public class WavContentTypeDefinition() : ContentTypeDefinition(Id)
{
	public const string Id = "wav";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinaryDataSource source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".wav"]));
}
