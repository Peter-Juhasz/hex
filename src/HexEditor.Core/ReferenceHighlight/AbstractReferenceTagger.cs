using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ReferenceHighlight;

public abstract class AbstractReferenceTagger(IViewAccessor viewAccessor) : ITagger<ReferenceTag>
{
	public Task<ImmutableArray<TagSpan<ReferenceTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var view = viewAccessor.View;
		var caret = view.Caret.Position.Point;
		SnapshotMismatchException.ThrowIfMismatch(caret.Snapshot, span.Snapshot);
		return GetTagsAsync(caret, span, cancellationToken);
	}

	protected abstract Task<ImmutableArray<TagSpan<ReferenceTag>>> GetTagsAsync(SnapshotPoint triggerPoint, SnapshotSpan span, CancellationToken cancellationToken);
}
