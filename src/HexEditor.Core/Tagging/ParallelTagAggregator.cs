using HexEditor.Core.Model;
using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Tagging;

public sealed class ParallelTagAggregator<TTag>(
	ImmutableArray<ITagger<TTag>> taggers
) 
	: ITagAggregator<TTag> where TTag : ITag
{
	public async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		if (taggers.IsEmpty)
		{
			return [];
		}

		if (taggers is [var single])
		{
			return await single.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		}

		var results = new ImmutableArray<TagSpan<TTag>>[taggers.Length];
		var tasks = new Task[taggers.Length];
		for (int i = 0; i < taggers.Length; i++)
		{
			tasks[i] = ParallelTagAggregator<TTag>.CollectAsync(results, i, taggers[i], span, cancellationToken);
		}

		await Task.WhenAll(tasks).ConfigureAwait(false);

		var totalCount = 0;
		for (int i = 0; i < results.Length; i++)
		{
			totalCount += results[i].Length;
		}
		var final = new TagSpan<TTag>[totalCount];
		var offset = 0;
		for (int i = 0; i < results.Length; i++)
		{
			var result = results[i];
			result.CopyTo(final, offset);
			offset += result.Length;
		}
		return ImmutableCollectionsMarshal.AsImmutableArray(final);
	}

	private static async Task CollectAsync(ImmutableArray<TagSpan<TTag>>[] results, int index, ITagger<TTag> tagger, SnapshotSpan span, CancellationToken cancellationToken)
	{
		try
		{
			results[index] = await tagger.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (SnapshotMismatchException)
		{
			throw;
		}
		catch { }
	}
}
