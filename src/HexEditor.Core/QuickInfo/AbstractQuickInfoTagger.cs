using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.QuickInfo;

public abstract class AbstractQuickInfoTagger : ITagger<QuickInfoTag>
{
	public Task<ImmutableArray<TagSpan<QuickInfoTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var triggerPoint = span.Start;
		return GetTagsAsync(triggerPoint, cancellationToken);
	}

	protected abstract Task<ImmutableArray<TagSpan<QuickInfoTag>>> GetTagsAsync(SnapshotPoint triggerPoint, CancellationToken cancellationToken);
}
