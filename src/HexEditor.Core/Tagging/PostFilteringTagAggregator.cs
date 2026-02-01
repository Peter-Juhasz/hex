using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class PostFilteringTagAggregator<TTag>(
	ITagAggregator<TTag> inner
)
	: ITagAggregator<TTag> where TTag : ITag
{
	public ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tagsTask = inner.GetTagsAsync(span, cancellationToken);
		if (tagsTask.IsCompletedSuccessfully)
		{
			return new(tagsTask.Result.OverlapsWith(span));
		}

		return GetTagsAsync(tagsTask, span);
	}

	private static async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(ValueTask<ImmutableArray<TagSpan<TTag>>> task, SnapshotSpan span)
	{
		var tags = await task.ConfigureAwait(false);
		return tags.OverlapsWith(span);
	}
}