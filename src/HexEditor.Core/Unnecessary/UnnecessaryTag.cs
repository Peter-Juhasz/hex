using HexEditor.Core.Tagging;

namespace HexEditor.Core.Unnecessary;

public record class UnnecessaryTag : ITag
{
	public static readonly UnnecessaryTag Instance = new();
}
