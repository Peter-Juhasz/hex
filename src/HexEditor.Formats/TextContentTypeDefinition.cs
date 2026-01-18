using HexEditor.Core.Model;

namespace HexEditor.Formats.Text;

public abstract class TextContentTypeDefinition() : ContentTypeDefinition(Id)
{
	public const string Id = "text";
}
