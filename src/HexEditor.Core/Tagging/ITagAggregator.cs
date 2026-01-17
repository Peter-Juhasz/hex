using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public interface ITagAggregator<T> where T : ITag
{
	ValueTask<ImmutableArray<TagSpan<T>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}
