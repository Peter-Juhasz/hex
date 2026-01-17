using HexEditor.Model;

namespace HexEditor.Core.Tagging;

public interface ITagAggregator<T> where T : ITag
{
	IAsyncEnumerable<TagSpan<T>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}