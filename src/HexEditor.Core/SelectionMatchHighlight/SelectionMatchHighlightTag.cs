using HexEditor.Core.Tagging;

namespace HexEditor.Core.SelectionMatchHighlight;

public record class SelectionMatchHighlightTag() : ITag
{
	public static readonly SelectionMatchHighlightTag Instance = new();
}
