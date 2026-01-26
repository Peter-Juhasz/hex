using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Tagging;

public interface ITagger<TTag> where TTag : ITag
{
	Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}
