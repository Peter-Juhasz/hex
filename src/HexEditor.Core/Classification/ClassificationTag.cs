using HexEditor.Core.Tagging;

namespace HexEditor.Core.Classification;

public record class ClassificationTag(string Type) : ITag
{
	public static readonly ClassificationTag KeywordTag = new("keyword");
}
