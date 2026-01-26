using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class PostFilteringTagAggregator<TTag>(
	ITagAggregator<TTag> inner
)
	: ITagAggregator<TTag> where TTag : ITag
{
	public async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tags = await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		for (var i = 0; i < tags.Length; i++)
		{
			if (!tags[i].Span.Span.OverlapsWith(span.Span))
			{
				using var result = new PooledArrayBuilder<TagSpan<TTag>>();

				if (i > 0)
				{
					result.AddRange(tags.AsSpan(..i));
				}

				for (var j = i + 1; j < tags.Length; j++)
				{
					var tag = tags[j];
					if (tag.Span.Span.OverlapsWith(span.Span))
					{
						result.Add(tag);
					}
				}
				return result.ToImmutableArray();
			}
		}
		return tags;
	}
}