using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public interface ITagger<T> where T : ITag
{
	Task<ImmutableArray<TagSpan<T>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}

public class EmptyTagger<T> : ITagger<T> where T : ITag
{
	public Task<ImmutableArray<TagSpan<T>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken) => Task.FromResult(ImmutableArray<TagSpan<T>>.Empty);
}