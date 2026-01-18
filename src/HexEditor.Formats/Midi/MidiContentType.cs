using HexEditor.Core.Model;
using HexEditor.Model;

namespace HexEditor.Formats.Midi;

public class MidiContentTypeDefinition() : ContentTypeDefinition(Id)
{
	public const string Id = "midi";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinaryDataSource source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".mid", ".midi"]));
}
