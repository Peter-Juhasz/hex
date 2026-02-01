using HexEditor.Core.Tagging;

namespace HexEditor.Core.Fields;

public record class FieldTag : ITag
{
	public static readonly FieldTag Instance = new();
}
