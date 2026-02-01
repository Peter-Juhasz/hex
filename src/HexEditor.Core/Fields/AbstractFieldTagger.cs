using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Fields;

public abstract class AbstractFieldTagger(IViewAccessor viewAccessor) : ITagger<FieldTag>
{
	public Task<ImmutableArray<TagSpan<FieldTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var view = viewAccessor.View;
		var caret = view.Caret.Position.Point;
		SnapshotMismatchException.ThrowIfMismatch(caret.Snapshot, span.Snapshot);
		return GetTagsAsync(caret, span, cancellationToken);
	}

	protected abstract Task<ImmutableArray<TagSpan<FieldTag>>> GetTagsAsync(SnapshotPoint triggerPoint, SnapshotSpan span, CancellationToken cancellationToken);
}
