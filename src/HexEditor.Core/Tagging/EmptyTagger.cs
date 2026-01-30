using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class EmptyTagger<TTag> : ITagger<TTag> where TTag : ITag
{
	private static readonly Task<ImmutableArray<TagSpan<TTag>>> _emptyResult = Task.FromResult(ImmutableArray<TagSpan<TTag>>.Empty);

	public Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken) => _emptyResult;
}