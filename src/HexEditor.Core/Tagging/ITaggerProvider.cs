namespace HexEditor.Core.Tagging;

public interface ITaggerProvider
{
	IEnumerable<ITagger<TTag>> CreateTaggers<TTag>(string contentType) where TTag : ITag;
}
