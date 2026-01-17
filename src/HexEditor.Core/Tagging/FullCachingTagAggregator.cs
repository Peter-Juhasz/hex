using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Tagging;

public sealed class FullCachingTagAggregator<TTag>(
	ITagAggregator<TTag> inner
)
	: ITagAggregator<TTag> where TTag : ITag
{
	private CacheItem? _lastCached;

	public async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var currentSnapshot = span.Snapshot;
		if (_lastCached is not { } cacheItem || cacheItem.Snapshot != currentSnapshot)
		{
			var tags = await inner.GetTagsAsync(currentSnapshot.Span, cancellationToken);
			cacheItem = new CacheItem(currentSnapshot, ImmutableCollectionsMarshal.AsArray(tags) ?? []);
			_lastCached = cacheItem;
		}
		var result = ImmutableArray.CreateBuilder<TagSpan<TTag>>();
		foreach (var tag in cacheItem.Tags)
		{
			if (tag.Span.Span.OverlapsWith(span.Span))
			{
				result.Add(tag);
			}
		}
		return result.ToImmutable();
	}

	private record class CacheItem(IBinarySnapshot Snapshot, TagSpan<TTag>[] Tags);
}