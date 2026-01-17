using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public sealed class EmptyTagAggregator<TTag> : ITagAggregator<TTag> where TTag : ITag
{
	public ValueTask<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken) => new([]);
}
