using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public interface ITaggerProvider
{
	ImmutableArray<ITagger<TTag>> CreateTaggers<TTag>(ImmutableArray<string> contentTypes) where TTag : ITag;
}
