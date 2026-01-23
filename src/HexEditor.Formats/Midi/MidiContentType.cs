using HexEditor.Core.ContentType;
using HexEditor.Formats.Text;
using HexEditor.Model;

namespace HexEditor.Formats.Midi;

public class MidiContentTypeDefinition() : ContentTypeDefinition(Id, baseType: BinaryContentTypeDefinition.Id)
{
	public const string Id = "midi";

	public override ValueTask<bool> MatchesAsync(string? filePath, IBinarySnapshot source, CancellationToken cancellationToken) =>
		new(MatchByExtension(filePath, [".mid", ".midi"]));
}
