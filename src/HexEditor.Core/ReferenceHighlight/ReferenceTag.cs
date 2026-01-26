using HexEditor.Core.Tagging;

namespace HexEditor.Core.ReferenceHighlight;

public record class ReferenceTag() : ITag
{
	public static readonly ReferenceTag ReferenceDefinitionTag = new();

	public static readonly ReferenceTag ReferenceUsageTag = new();
}
