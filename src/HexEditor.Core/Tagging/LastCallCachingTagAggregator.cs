using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class LastCallCachingTagAggregator<TTag>(
	ITagAggregator<TTag> inner
)
	: ITagAggregator<TTag> where TTag : ITag
{
	private CacheItem? _lastCached;

	public ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		if (_lastCached is { } cacheItem && cacheItem.Span == span)
		{
			return new(cacheItem.Tags);
		}

		return GetCoreAsync(span, cancellationToken);
	}

	private async ValueTask<ImmutableArray<TagSpan<TTag>>> GetCoreAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tags = await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		var cacheItem = new CacheItem(span, tags);
		_lastCached = cacheItem;
		return tags;
	}

	private record class CacheItem(SnapshotSpan Span, ImmutableArray<TagSpan<TTag>> Tags);
}
