using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public interface ITagger<TTag> where TTag : ITag
{
	Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}

public class EmptyTagger<TTag> : ITagger<TTag> where TTag : ITag
{
	public Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken) => Task.FromResult(ImmutableArray<TagSpan<TTag>>.Empty);
}