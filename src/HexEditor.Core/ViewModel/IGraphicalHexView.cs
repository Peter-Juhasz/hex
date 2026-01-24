using HexEditor.Core.Caret;
using HexEditor.Core.Model;
using HexEditor.Core.Scrolling;
using HexEditor.Core.Selection;
using HexEditor.Model;
using System.Collections.Immutable;
using System.Numerics;

namespace HexEditor.Core.ViewModel;

public interface IGraphicalHexView
{
	IBinarySnapshot Snapshot { get; }

	ImmutableArray<IHexViewRow> VisibleRows { get; }

	ISelection Selection { get; }

	ICaret Caret { get; }

	IViewport Viewport { get; }

	double ScrollableHeight { get; }

	event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	SnapshotSpan GetContainingRow(SnapshotPoint point);

	SnapshotPoint MapFromVisualHex(Vector2 point);
	SnapshotSpan MapRowFromVisual(double verticalOffset);
	long MapRowIndexFromVerticalOffset(double verticalOffset);

	ViewportBounds MapToVisualAscii(SnapshotPoint point);
	Vector2[] MapToVisualAscii(SnapshotSpan span);
	ViewportBounds MapToVisualHex(SnapshotPoint point);
	Vector2[] MapToVisualHex(SnapshotSpan span);
}
