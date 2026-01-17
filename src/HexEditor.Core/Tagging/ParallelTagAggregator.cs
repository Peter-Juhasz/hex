using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Tagging;

public class ParallelTagAggregator<TTag>(
	ImmutableArray<ITagger<TTag>> taggers
) 
	: ITagAggregator<TTag> where TTag : ITag
{
	public async ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var results = new ImmutableArray<TagSpan<TTag>>[taggers.Length];
		var tasks = new Task[taggers.Length];
		for (int i = 0; i < taggers.Length; i++)
		{
			tasks[i] = ParallelTagAggregator<TTag>.CollectAsync(results, i, taggers[i], span, cancellationToken);
		}

		await Task.WhenAll(tasks);

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
			results[index] = await tagger.GetTagsAsync(span, cancellationToken);
		}
		catch { }
	}
}
