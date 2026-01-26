using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class EmptyTagger<TTag> : ITagger<TTag> where TTag : ITag
{
	public Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken) => Task.FromResult(ImmutableArray<TagSpan<TTag>>.Empty);
}