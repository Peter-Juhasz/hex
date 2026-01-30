using HexEditor.Core.Model;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class LastCallWithEditorStateCachingTagAggregator<TTag>(
	ITagAggregator<TTag> inner,
	IViewAccessor viewAccessor
)
	: ITagAggregator<TTag> where TTag : ITag
{
	private CacheItem? _lastCached;

	public ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var caret = GetState();

		if (_lastCached is { } cacheItem && cacheItem.Span == span && cacheItem.Caret == caret)
		{
			return new(cacheItem.Tags);
		}

		return GetCoreAsync(span, cancellationToken);
	}

	private SnapshotPoint GetState() => viewAccessor.View.Caret.Position.Point;

	private async ValueTask<ImmutableArray<TagSpan<TTag>>> GetCoreAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var caret = GetState();
		var tags = await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		var cacheItem = new CacheItem(span, caret, tags);
		_lastCached = cacheItem;
		return tags;
	}


	private record class CacheItem(SnapshotSpan Span, SnapshotPoint Caret, ImmutableArray<TagSpan<TTag>> Tags);
}
