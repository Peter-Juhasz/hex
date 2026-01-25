using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class LockingTagAggregator<TTag>(
	ITagAggregator<TTag> inner
)
	: ITagAggregator<TTag> where TTag : ITag
{
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			return await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}