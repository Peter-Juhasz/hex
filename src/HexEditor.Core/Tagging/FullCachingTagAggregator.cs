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

	public ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var currentSnapshot = span.Snapshot;
		if (_lastCached is { } cacheItem && cacheItem.Snapshot == currentSnapshot)
		{
			using var result = new PooledArrayBuilder<TagSpan<TTag>>();
			foreach (var tag in cacheItem.Tags)
			{
				if (tag.Span.Span.OverlapsWith(span.Span))
				{
					result.Add(tag);
				}
			}
			return new(result.ToImmutableArray());
		}

		return GetCoreAsync(span, cancellationToken);
	}

	private async ValueTask<ImmutableArray<TagSpan<TTag>>> GetCoreAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var currentSnapshot = span.Snapshot;
		var tags = await inner.GetTagsAsync(currentSnapshot.Span, cancellationToken).ConfigureAwait(false);
		var cacheItem = new CacheItem(currentSnapshot, ImmutableCollectionsMarshal.AsArray(tags) ?? []);
		_lastCached = cacheItem;

		using var result = new PooledArrayBuilder<TagSpan<TTag>>();
		foreach (var tag in cacheItem.Tags)
		{
			if (tag.Span.Span.OverlapsWith(span.Span))
			{
				result.Add(tag);
			}
		}
		return result.ToImmutableArray();
	}

	private record class CacheItem(IBinarySnapshot Snapshot, TagSpan<TTag>[] Tags);
}
